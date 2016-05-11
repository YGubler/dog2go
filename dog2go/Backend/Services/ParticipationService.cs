﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using dog2go.Backend.Model;
using dog2go.Backend.Repos;

namespace dog2go.Backend.Services
{
    public class ParticipationService
    {
        public static void AddParticipation(GameTable table, string curUser)
        {
            Participation newParticipation;
            if (table.Participations.Count() % 2 == 1)
            {
                User actualUser = UserRepository.Instance.Get()
                        .First(user => user.Value.Nickname == curUser).Value;
                newParticipation =
                    new Participation(actualUser)
                    {
                        Partner = table.Participations.Last().Participant
                    };
                table.Participations.Last().Partner = actualUser;
            }
            else
            {
                newParticipation = new Participation(UserRepository.Instance.Get().First(user => user.Value.Nickname == curUser).Value);
            }
            table.PlayerFieldAreas.Find(area => area.Identifier == table.Participations.Count() + 1).Participation = newParticipation;
            table.Participations.Add(newParticipation);
        }

        public static Participation GetParticipation(GameTable table, string curUser)
        {
            return table?.Participations?.FirstOrDefault(part => curUser.Equals(part.Participant.Nickname));
        }
    }


}