﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using dog2go.Backend.Constants;
using dog2go.Backend.Interfaces;
using dog2go.Backend.Model;
using dog2go.Backend.Repos;
using dog2go.Backend.Services;
using Microsoft.AspNet.SignalR;
using WebGrease.Css.Extensions;

namespace dog2go.Backend.Hubs
{
    [Authorize]
    public class GameHub : GenericHub
    {
        private static readonly object Locker = new object();
        

        public GameHub(IGameRepository repos) : base(repos) { }

        public GameHub() { }


        public GameTable ConnectToTable(int gameTableId)
        {
            lock (Locker)
            {
                GameTable table = GameTableService.GetTable(Games, gameTableId);
                string curUser = Context.User.Identity.Name;
                List<HandCard> cards = null;

                if (GameTableService.AlreadyConnected(table, curUser))
                {
                    Participation participation = ParticipationService.GetParticipation(table, curUser);

                    if (table.Participations.Count == GlobalDefinitions.NofParticipantsPerTable && !table.IsInitialized)
                    {
                        AllConnected(table);
                        Clients.Client(Context.ConnectionId).createGameTable(table, table.Identifier);
                    }
                    else
                    {
                        cards = table.CardServiceData?.GetActualHandCards(participation.Participant, table);
                        Task task = Clients.Client(Context.ConnectionId).backToGame(table, cards, table.Identifier);
                        task.Wait();
                    }
                    if (table.ActualParticipation == participation)
                    {
                        NotifyActualPlayer(participation.Participant, cards, table.Identifier);
                    }
                }
                else
                {
                    ParticipationService.AddParticipation(table, curUser);
                    Clients.Client(Context.ConnectionId).createGameTable(table);
                }
                return table;
            }
        }

        // for test method calls only
        public GameTable GetGeneratedGameTable(int tableId)
        {
            int gameTableId = GameFactory.CreateGameTable(Games, tableId.ToString());
            lock (Locker)
            {
                return Games.Get().Find(table => table.Identifier.Equals(gameTableId));
            }
        }

        public void SendCards(List<HandCard> cards, User user, int tableId)
        {
            user.ConnectionIds.ForEach(id =>
            {
                Clients.Client(id).assignHandCards(cards, tableId);
            });
        }
        private void NotifyActualPlayer(User user, List<HandCard> handCards, int tableId)
        {
            GameTable actualGameTable = GameTableService.GetTable(Games, tableId);
            List<HandCard> validCards = actualGameTable.CardServiceData.ProveCards(handCards, actualGameTable, user);
            IHubContext context = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
            if (validCards.Find(card => card.IsValid) != null)
            {
                Task firstTask = context.Clients.Group(tableId.ToString(), user.ConnectionIds.ToArray()).broadcastSystemMessage(ServerMessages.InformOtherPlayer.Replace("{0}", user.Nickname), tableId, DateTime.Now.Ticks + GetMessageCounter());
                firstTask.Wait();
                actualGameTable.ActualParticipation = ParticipationService.GetParticipation(actualGameTable, user.Nickname);
                user.ConnectionIds.ForEach(cId =>
                {
                    Task task =  context.Clients.Client(cId).broadcastSystemMessage(ServerMessages.NofityActualPlayer, actualGameTable.Identifier, DateTime.Now.Ticks + GetMessageCounter());
                    task.Wait();
                    ColorCode colorCode = GameTableService.GetColorCodeForUser(Games, GameTableService.AreAllEndFieldsUsedForColorCode(actualGameTable,
                        GameTableService.GetColorCodeForUser(Games, user.Nickname, tableId)) ? 
                        ParticipationService.GetPartner(user, actualGameTable.Participations).Nickname : user.Nickname, tableId);
                    Clients.Client(cId).notifyActualPlayer(validCards,colorCode , tableId);
                });
            }
            else
            {
                NotifyNextPlayer("", actualGameTable);
            }
        }

        public void AllConnected(GameTable table)
        {
            table.RegisterCardService(new CardServices());
            table.IsInitialized = true;
            SendCardsForRound(table);
        }

        private void SendCardsForRound(GameTable table)
        {
            IHubContext context = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
            context.Clients.Group(table.Identifier.ToString()).broadcastStateMessage(ServerMessages.NewRoundStart, table.Identifier, DateTime.Now.Ticks + GetMessageCounter());
            GameTableService.UpdateActualRoundCards(table);
            foreach (var participation in table.Participations)
            {
                SendCards(participation.ActualPlayRound.Cards, participation.Participant, table.Identifier);
                participation.Participant.CardDropped = false;
            }

            if (table.ActualParticipation != null)
                NotifyActualPlayer(table.ActualParticipation.Participant, table.ActualParticipation.ActualPlayRound.Cards, table.Identifier);
            else
            {
                Participation participation = table.Participations.First();
                if (participation != null)
                {
                    NotifyActualPlayer(participation.Participant, participation.ActualPlayRound.Cards, table.Identifier);
                }
            }
        }

        // this method is not used yet, as initial card excchange has not been implemented
        public void ChooseCardExchange(HandCard selectedCard, int tableId)
        {
            GameTable actualGameTable = GameTableService.GetTable(Games, tableId);
            User actualUser = actualGameTable.Participations.Find(participation => participation.Participant.Identifier == Context.User.Identity.Name).Participant;
            actualGameTable.CardServiceData.CardExchange(actualUser, ref actualGameTable, selectedCard);
            User partnerUser = ParticipationService.GetPartner(actualUser, actualGameTable.Participations);
            partnerUser.ConnectionIds.ForEach(id =>
            {
                Clients.Client(id).exchangeCard(selectedCard);
            });
        }

        public void ValidateMove(MeepleMove meepleMove, CardMove cardMove, int tableId)
        {
            GameTable actualGameTable = GameTableService.GetTable(Games, tableId);
            if (meepleMove == null)
                return;
            meepleMove.Meeple.CurrentPosition = meepleMove.Meeple.CurrentPosition ?? Validation.GetFieldById(actualGameTable, meepleMove.Meeple.CurrentFieldId);
            meepleMove.MoveDestination = meepleMove.MoveDestination ?? Validation.GetFieldById(actualGameTable, meepleMove.DestinationFieldId);
            if (Validation.ValidateMove(meepleMove, cardMove))
            {
                GameTableService.UpdateMeeplePosition(meepleMove, actualGameTable, cardMove.SelectedAttribute != null);
                List<Meeple> allMeeples = new List<Meeple>();
                foreach (PlayerFieldArea area in actualGameTable.PlayerFieldAreas)
                {
                    allMeeples.AddRange(area.Meeples);
                }
                actualGameTable.CardServiceData.RemoveCardFromUserHand(actualGameTable, GameTableService.GetActualUser(Context.User.Identity.Name), cardMove.Card);
                Task sendPosition = Clients.All.sendMeeplePositions(allMeeples, tableId);
                sendPosition.Wait();
                if (GameServices.IsGameFinished(actualGameTable))
                {
                    IEnumerable<string> winners = GameServices.GetWinners(actualGameTable);
                    string winMsg = ServerMessages.GameFinished.Replace("{0}", string.Join(" & ", winners));
                    Clients.All.notifyAllGameIsFinished(winMsg, actualGameTable.Identifier);
                }
                else
                {
                    NotifyNextPlayer("", actualGameTable);
                }
            }
            else
            {
                Clients.Caller.returnMove(tableId);
            }
        }

        private void NotifyNextPlayer(string nextUserName, GameTable actualGameTable)
        {
            string nextPlayerNickname;
            if (string.IsNullOrWhiteSpace(nextUserName))
            {
                nextPlayerNickname = ParticipationService.GetNextPlayer(actualGameTable, Context.User.Identity.Name);
            }
            else
            {
                nextPlayerNickname = nextUserName;
            }
            User nextUser = UserRepository.Instance.Get().FirstOrDefault(user => user.Value.Nickname == nextPlayerNickname).Value;
            List<HandCard> cards = actualGameTable.CardServiceData.GetActualHandCards(nextUser, actualGameTable);
            List<HandCard> validHandCards = actualGameTable.CardServiceData.ProveCards(cards, actualGameTable, nextUser);
            IHubContext context = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
            actualGameTable.ActualParticipation = ParticipationService.GetParticipation(actualGameTable, nextUser.Nickname);
            Task task = context.Clients.Group(actualGameTable.Identifier.ToString(), nextUser.ConnectionIds.ToArray()).broadcastSystemMessage(ServerMessages.InformOtherPlayer.Replace("{0}", nextUser.Nickname), actualGameTable.Identifier, DateTime.Now.Ticks + GetMessageCounter());
            task.Wait();
            if (validHandCards.Count > 0 && validHandCards.Find(card => card.IsValid) == null)
            {
                if (!nextUser.CardDropped)
                {
                    context.Clients.Group(actualGameTable.Identifier.ToString(), nextUser.ConnectionIds.ToArray()).broadcastStateMessage(ServerMessages.NoValidCardAvailable.Replace("{0}", nextUser.Nickname), actualGameTable.Identifier, DateTime.Now.Ticks + GetMessageCounter());
                    nextUser.ConnectionIds.ForEach(id => context.Clients.Client(id).broadcastStateMessage(ServerMessages.YourCardsHaveBeenDropped, actualGameTable.Identifier, DateTime.Now.Ticks + GetMessageCounter()));
                } 
            }
                
            nextUser.ConnectionIds.ForEach(id =>  
            {
                if (validHandCards.Find(card => card.IsValid) != null)
                {
                    Task chatTask =  context.Clients.Client(id).broadcastSystemMessage(ServerMessages.NofityActualPlayer, actualGameTable.Identifier, DateTime.Now.Ticks + GetMessageCounter());
                    chatTask.Wait();
                    ColorCode colorCode = GameTableService.GetColorCodeForUser(Games, GameTableService.AreAllEndFieldsUsedForColorCode(actualGameTable,
                        GameTableService.GetColorCodeForUser(Games, nextUser.Nickname, actualGameTable.Identifier)) ?
                        ParticipationService.GetPartner(nextUser, actualGameTable.Participations).Nickname : nextUser.Nickname, actualGameTable.Identifier);
                    Clients.Client(id).notifyActualPlayer(validHandCards, colorCode, actualGameTable.Identifier);
                }    
                else
                {
                    actualGameTable.CardServiceData.RemoveAllCardsFromUser(actualGameTable,nextUser );
                    nextUser.CardDropped = true;
                    Clients.Client(id).dropCards(actualGameTable.Identifier);

                    if (actualGameTable.CardServiceData.ProveCardsCount%GlobalDefinitions.NofParticipantsPerTable != 0)
                    {
                        NotifyNextPlayer(ParticipationService.GetNextPlayer(actualGameTable, nextUser.Nickname), actualGameTable);
                        return;
                    }

                    if (!actualGameTable.CardServiceData.AreCardsOnHand(actualGameTable))
                    {
                        SendCardsForRound(actualGameTable);
                    }
                    else
                    {
                        NotifyNextPlayer(ParticipationService.GetNextPlayer(actualGameTable, nextUser.Nickname), actualGameTable);
                    }
                }
            });
        }
    }
}
