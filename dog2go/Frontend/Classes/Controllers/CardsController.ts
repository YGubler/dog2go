﻿////<reference path="../../Library/JQuery/jqueryui.d.ts"/>

import gfs = require("../Services/GameFieldsService");
import GameFieldService = gfs.GameFieldService;

import rs = require("../Services/RoundService");
import RoundService = rs.RoundService;

import ts = require("../Services/TurnService");
import TurnService = ts.TurnService;

import mc = require("./MeepleController");
import MeepleController = mc.MeepleController;

export class CardsController {
    private gameFieldService: GameFieldService;
    private turnService: TurnService;
    private roundService: RoundService;
    private myCards: ICard[];
    private selctedCard: ICard = null;
    private meepleController: MeepleController;


    constructor(meepleController: MeepleController) {
        this.meepleController = meepleController;

        this.gameFieldService = GameFieldService.getInstance();
        this.gameFieldService.assignHandCardsCB = this.showHandCards.bind(this);
        this.roundService = RoundService.getInstance();
        this.roundService.assignHandCardsCB = this.showHandCards.bind(this);
        this.turnService = TurnService.getInstance();
        this.turnService.notifyActualPlayerCardsCB = this.notifyActualPlayer.bind(this);

        this.makeGamefieldDroppable();
    }

    public showHandCards(cards: ICard[]) {
        this.myCards = cards;
        console.log("Show HandCards: ", cards);
        
        if (cards !== null) {
            for (let i = 0; i < cards.length; i++) {
                this.addCard(cards[i]);
                this.setDragableOnCard(cards[i]);
                this.disableDrag(cards[i]);
            }
        }
    }

    public notifyActualPlayer(possibleCards: IHandCard[], meepleColor: number) {
        this.dropAllCards();
        this.showHandCards(possibleCards);
        for (var card of possibleCards) {
            if (card.IsValid) {
                this.enableDrag(card);
            }
        }
    }

    public addCard(card: ICard) {
        var container = $("#cardContainer");
        container.append(`<img class="handcards" id="${card.Name}" src="/Frontend/Images/cards-min/${card.ImageIdentifier}" ></img>`);
    }

    public makeGamefieldDroppable() {
        $("#gameContent > canvas").droppable({
            accept: ".handcards",
            drop: (event, ui) => {
                var id: string = ui.draggable.attr("id");
                var card: ICard = this.getFirstCardsByName(id);
                // TODO: Handle Atribute-Selection?
                var cardMove: ICardMove = { Card: card, SelectedAttribute: card.Attributes[0] };
                this.selctedCard = card;
                this.meepleController.proceedMeepleTurn(cardMove);
                console.log("verify the following card: ", id, card);
            }
        });
    }

    public dropAllCards() {
        $(".handcards").remove();
    }

    public disableAllDrag() {
        $(`.handcards`).draggable('disable');
    }

    public disableDrag(card: ICard) {
        $(`#${card.Name}`).draggable('disable');
    }

    public enableDrag(card: ICard) {
        $(`#${card.Name}`).draggable('enable');
    }

    public setDragableOnCard(card: ICard) {
        // HowTo draggable: http://stackoverflow.com/questions/5735270/revert-a-jquery-draggable-object-back-to-its-original-container-on-out-event-of
        $(`#${card.Name}`).draggable({
            revert: function (event, ui) {
                console.log($(this).data("ui-draggable"));
                $(this).data("ui-draggable").originalPosition = {
                    top: 0,
                    left: 0
                };
                return !event;
            }
        });
    }

    private getFirstCardsByName(name: string) :ICard {
        for (var card of this.myCards) {
            if (name === card.Name) {
                return card;
            }
        }
        return null;
    }
}