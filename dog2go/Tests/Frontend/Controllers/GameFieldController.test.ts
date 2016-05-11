﻿import _phaser = require("phaser");
//import Area = require("../../../Frontend/Classes/Controllers/GameArea");
import gfc = require("../../../Frontend/Classes/Controllers/GameFieldsController");
import GameFieldController = gfc.GameFieldController;
import gm = require("../../../Frontend/Classes/Model/GameModel");
import Coordinates = require("../../../Frontend/Classes/Controllers/FieldCoordinates");
import FieldCoordinatesData = Coordinates.FieldCoordinatesData;

describe("GameFieldController - ", () => {
    var timerCallback: jasmine.Spy;
    var game: Phaser.Game;
    beforeEach(() => {
        timerCallback = jasmine.createSpy("timerCallback");
        jasmine.clock().install();

        console.log(_phaser);
        game = new Phaser.Game();
    });

    afterEach(() => {
        jasmine.clock().uninstall();
    });

    it("creates Kennel fields at the right position", () => {
        var gameFieldController = new GameFieldController(game, 2);

        expect(true).toBe(true);
        var data: IKennelField[] = [<any>{ kennelfield: 1 }, <any>{ kennelfield: 2 }, <any>{ kennelfield: 3 }, <any>{ kennelfield: 4 }];
        var span = 30;
        var xStart = [380, 20, 140, 500];
        var yStart = [20, 140, 500, 380];
        var fc = new FieldCoordinatesData(span, xStart, yStart);
        var ac = new Coordinates.AreaCoordinates(0, fc);
        setTimeout(() => {
            gameFieldController.addKennelFields(data, ac, 0xFF00CC);
        }, 0);
        jasmine.clock().tick(0);
        var pos0 = data[0].viewRepresentation.position;
        var pos1 = data[1].viewRepresentation.position;
        var pos2 = data[2].viewRepresentation.position;
        var pos3 = data[3].viewRepresentation.position;
        console.log(pos0, pos1, pos2, pos3);
        expect(pos0.x).toEqual(50);
        expect(pos0.y).toEqual(20);
        expect(pos1.x).toEqual(20);
        expect(pos1.y).toEqual(20);
        expect(pos2.x).toEqual(50);
        expect(pos2.y).toEqual(50);
        expect(pos3.x).toEqual(20);
        expect(pos3.y).toEqual(50);
    }, 10000);
});