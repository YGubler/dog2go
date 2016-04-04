/// <reference path="../../Library/Phaser/phaser.comments.d.ts"/>

enum AreaColor {
    Red = 0xff0000,
    Blue = 0x0000ff,
    Green = 0x00ff00,
    Yellow = 0xedc613
}

class MoveDestinationField {
    identifier: number; 
    private previous: MoveDestinationField;
    private next: MoveDestinationField;

    NextIdentifier : number;
 
    PreviousIdentifier:number;
    
    viewRepresentation;

    constructor(previous: MoveDestinationField) {
        this.previous = previous;
        let self = this;
        if (previous instanceof StartField && self instanceof EndField) {
            previous.setEndFieldEntry(self);
        } else if (previous != null) {
            previous.setNext(self);
        } 
    }

    private setNext(next: MoveDestinationField) {
        this.next = next;
    }
}

class EndField extends MoveDestinationField {
    constructor(previous: MoveDestinationField) {
        super(previous);
    }
}

class StartField extends MoveDestinationField {
    constructor(previous: MoveDestinationField) {
        super(previous);
    }
    endFieldEntry: EndField;
    setEndFieldEntry(next: EndField) {
        this.endFieldEntry = next;
    }
}


class Persontest {
    private firstName: string;
    private lastName: string;

    setFirstName(value: string) {
        this.firstName = value;
    }

    setLastName(value: string) {
        this.lastName = value;
}

    getFullName(lastNameFirst: boolean = false): string {
        if (lastNameFirst) {
            return this.lastName + ", " + this.firstName;
        }
        return this.firstName + ", " + this.lastName;
    }
}

class PlayerFieldArea {
    constructor(color: AreaColor) {
        this.color = color;
        this.createFields();
    }

    color: AreaColor;
    //kennelFields: MoveDestinationField[];
    Fields: MoveDestinationField[] = [];
    endFields: EndField[] = [];

    private createFields() {
        let prev = null;
        let field: MoveDestinationField;
        let i: number;
        // create the 4 fields before the start field
        for (i = 0; i < 4; i++) {
            field = new MoveDestinationField(prev);
            this.Fields.push(field);
            prev = field;
        }
        // create the start field itself
        let startField = new StartField(prev);
        this.Fields.push(startField);
        // create the 11 fields after the start field
        prev = startField;
        for (i = 0; i < 11; i++) {
            field = new MoveDestinationField(prev);
            this.Fields.push(field);
            prev = field;
        }
        // create the 4 end fields 
        prev = startField;
        for (i = 0; i < 4; i++) {
            field = new EndField(prev);
            this.endFields.push(field);
            prev = field;
        }
    }
}

function addTestData(): PlayerFieldArea[] {
    let areas: PlayerFieldArea[] = [];
    const colors: AreaColor[] = [AreaColor.Red, AreaColor.Blue, AreaColor.Yellow, AreaColor.Green];
    for (let i = 0; i < 4; i++) {
        const area = new PlayerFieldArea(colors[i]);
        areas.push(area);
    }
    return areas;
}






class GameArea extends Phaser.State {
    constructor() {
        super();
        this.gameFieldService = GameFieldService.getInstance(this.buildFields.bind(this));
        const gameStates = {
            preload: this.preload,
            create: this.create
        };
        this.game = new Phaser.Game(720, 720, Phaser.AUTO, "content", gameStates);
        this.game.state.add('GameArea', this, false);

        this.game.state.start('GameArea');
    }

    gameFieldService: GameFieldService;
    game:Phaser.Game;
    areas: PlayerFieldArea[] = [];
    fields: Phaser.Graphics[] = [];

    /* load game assets here, but not objects */
    preload() {
        this.areas = addTestData();
        this.fields = [];
        
        this.game.load.image('meeple_blue', '../Frontend/Images/pawn_blue.png');

    }

    public static getFieldById(id: number, fields: MoveDestinationField[]) {
        console.log('Geeting fild:', id);
        for (var field of fields) {
            if (id == field.Identifier) {
                return field;
            }
        }
        console.log('No Field Found by ID in Area', id, fields);
    }

    public buildFields(areasPar : PlayerFieldArea[]) {
        var game = this.game;
        var cellSpan = 40;
        this.game.stage.backgroundColor = 0xddeeCC;
        let pos = 0;
        const xStart = [520, 40, 200, 680];
        const yStart = [40, 200, 680, 520];
        const x1 = [-cellSpan, 0, cellSpan, 0];
        const y1 = [0, cellSpan, 0, -cellSpan];
        const x2 = [0, cellSpan, 0, -cellSpan];
        const y2 = [cellSpan, 0, -cellSpan, 0];

        //let area = this.areas[2];
        for (let area of areasPar) {
            let el = area.Fields[0];
            let x = xStart[pos];
            let y = yStart[pos];
            for (let i = 0; i < area.Fields.length; i++) {
                var color = 0xeeeeee;
                if (el instanceof StartField) {
                    color = area.color;
                    let ex = x;
                    let ey = y;
                    let finEl = el.endFieldEntry;
                    for (let j = 0; j < area.endFields.length; j++) {
                        ex += x2[pos];
                        ey += y2[pos];
                        el.viewRepresentation = this.addField(game, ex, ey, color);
                        finEl = GameArea.getFieldById(finEl.NextIdentifier, area.Fields); //finEl.next;
                    }
                }
                if (el !== undefined) {
                    el.viewRepresentation = this.addField(game, x, y, color);
                }
                // Calculate Position for next field 
                if (i < 8 || i > 11) {
                    x += x1[pos];
                    y += y1[pos];
                } else {
                    x += x2[pos];
                    y += y2[pos];
                }

                el = GameArea.getFieldById(el.NextIdentifier, area.Fields);
            }
            pos++;
        }

        var meepleBlue = this.game.add.sprite(this.game.world.centerX, this.game.world.centerY, 'meeple_blue');
        meepleBlue.anchor.setTo(0.5, 0.5);
        meepleBlue.scale.setTo(0.08, 0.08);
        meepleBlue.inputEnabled = true;
        meepleBlue.input.enableDrag();
        meepleBlue.input.enableSnap(40, 40, false, true);
        console.log("meeples parent is: ", meepleBlue.parent);
        meepleBlue.events.onDragStop.add(this.dropLimiter, this);
    }

    private addField(game: Phaser.Game, x: number, y: number, color: number): Phaser.Graphics {
        let graphics = game.add.graphics(x, y); // positioning is relative to parent (in this case, to the game world as no parent is defined)
        graphics.beginFill(color, 1);
        graphics.drawCircle(0, 0, 20); //draw a circle relative to it's parent (in this case, the graphics object)
        graphics.endFill();
        this.fields.push(graphics);
        return graphics;
    }
    public dropLimiter(item: Phaser.Sprite) {
        var nearest: Phaser.Graphics;
        var smallest: number = 99999999;
        var pos = item.world;
        this.fields.forEach((field) => {
            var fieldPos = field.world;
            var dist = fieldPos.distance(pos, true);
            if (!(smallest < dist)) {
                smallest = dist;
                nearest = field;
            }
        });
        if (nearest != null) {
            
            item.x = nearest.x;
            item.y = nearest.y;
        }
    }

    //// This function is called when a full screen request comes in
    //onGoFullScreen() {
    //    // tell Phaser how you want it to handle scaling when you go full screen
    //    this.game.scale.fullScreenScaleMode = Phaser.ScaleManager.SHOW_ALL;
    //    // and this causes it to actually do it
    //    this.game.scale.refresh();
    //}
    //goFullScreen() {

    //}

    public create() {
        this.gameFieldService.getGameFieldData();
        /*$(function() {
            var gameHub = $.connection.gameHub;
            gameHub.client.doSomeShit = function() {
                console.log("doSomeShit: SHIT SHIT");
            }
            gameHub.client.createGameTable = function(areas) { //(areas) => {
                console.log("GameFieldService: Called createGameTable!", areas);
                //callback(areas);
            }

            $.connection.hub.start().done(() => {
                console.log("GameFieldService: Connection etablished");
                gameHub.server.sendGameTable();
            });
            $.connection.hub.error(error => {
                console.log('SignalR error: ' + error);
            });
        });*/
        /*GameFieldService.getGameFieldData((areas: PlayerFieldArea[]) => {
            console.log('Yeaaaaa got the fields');
            console.log(areas);
        });*/
        

        // would allow to go to fullscreen on desktop systems
        //this.game.scale.onFullScreenInit.add(GameArea.prototype.onGoFullScreen, this);        
        //this.game.input.onTap.add(() => { this.game.scale.startFullScreen(true); }, this);
    }
    
}

window.onload = () => {
    var gameArea = new GameArea();
};