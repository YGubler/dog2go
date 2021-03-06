using System.Collections.Generic;
using dog2go.Backend.Interfaces;
using dog2go.Backend.Model;

namespace dog2go.Backend.Services
{
    public static class GameFactory
    {
        private static GameTable GenerateNewGameTable(int gameId, string gameName)
        {
            List<PlayerFieldArea> areas = new List<PlayerFieldArea>();

            int id = 0;

            const int fieldId = 0;
            PlayerFieldArea areaTop = new PlayerFieldArea(++id, ColorCode.Blue, fieldId);
            PlayerFieldArea areaLeft = new PlayerFieldArea(++id, ColorCode.Red, areaTop.FieldId);
            PlayerFieldArea areaBottom = new PlayerFieldArea(++id, ColorCode.Green, areaLeft.FieldId);
            PlayerFieldArea areaRight = new PlayerFieldArea(++id, ColorCode.Yellow, areaBottom.FieldId);

            // Connection between PlayFieldAreas
            areaTop.Next = areaLeft;
            areaTop.Previous = areaRight;
            areaRight.Next = areaTop;
            areaRight.Previous = areaBottom;
            areaLeft.Next = areaBottom;
            areaLeft.Previous = areaTop;
            areaBottom.Next = areaRight;
            areaBottom.Previous = areaLeft;

            areas.Add(areaTop);
            areas.Add(areaLeft);
            areas.Add(areaBottom);
            areas.Add(areaRight);

            GameTable table = new GameTable(areas, gameId, gameName);
            return table;
        }

        public static int CreateGameTable(IGameRepository games, string tableName)
        {
            int newIdentifier = games.Get().Count;
            GameTable generatedTable = GenerateNewGameTable(newIdentifier, tableName);
            games.Add(generatedTable);
            return generatedTable.Identifier;
        }
    }
}