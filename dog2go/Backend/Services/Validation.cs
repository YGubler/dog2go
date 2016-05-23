﻿using dog2go.Backend.Model;

namespace dog2go.Backend.Services
{
    public static class Validation
    {
        public static bool ProveChangePlace(Meeple moveMeeple, MoveDestinationField destinationField)
        {
            if (moveMeeple == null || destinationField.CurrentMeeple == null)
                return false;
            if (IsSameColorCode(moveMeeple.ColorCode, destinationField.CurrentMeeple.ColorCode))
                return false;
            if (IsSimpleInvalidChangeField(destinationField) || IsSimpleInvalidChangeField(moveMeeple.CurrentPosition))
                return false;
            if (IsStandardField(destinationField) && !IsStandardField(moveMeeple.CurrentPosition))
                return IsValidStartField(moveMeeple.CurrentPosition);
            if (!IsStandardField(destinationField) && IsStandardField(moveMeeple.CurrentPosition))
                return IsValidStartField(destinationField);
            if (!(IsStandardField(moveMeeple.CurrentPosition) && IsStandardField(destinationField)))
                return IsValidStartField(destinationField) && IsValidStartField(moveMeeple.CurrentPosition);
            return true;
        }

        public static bool ProveLeaveKennel(Meeple moveMeeple, MoveDestinationField destinationField)
        {
            if (!moveMeeple.CurrentPosition.FieldType.Contains("KennelField"))
                return false;
            if (!destinationField.FieldType.Contains("StartField"))
                return false;
            StartField startField = destinationField as StartField;
            if (startField != null && !IsSameColorCode(startField.ColorCode, moveMeeple.ColorCode))
                return false;
            return destinationField.CurrentMeeple == null || IsValidStartField(destinationField);
        }

        public static bool ProveValueCard(Meeple moveMeeple, MoveDestinationField destinationField, int value)
        {
            MoveDestinationField currentPos = moveMeeple.CurrentPosition;
            if (destinationField == null) return false;
            if (HasBlockedField(currentPos, value))
                return false;
            if (destinationField.FieldType.Contains("EndField"))
                return CanMoveToEndFields(currentPos, value, moveMeeple.ColorCode);
            currentPos = GetNextField(currentPos, value);
            return currentPos?.Identifier == destinationField.Identifier;
        }

        public static bool IsSameColorCode(ColorCode firstColorCode, ColorCode secondColorCode)
        {
            return firstColorCode == secondColorCode;
        }

        public static bool IsMovableField(MoveDestinationField field)
        {
            return field.FieldType.Contains("StartField") || field.FieldType.Contains("EndField") ||
                   field.FieldType.Contains("StandardField");
        }

        public static bool IsStandardField(MoveDestinationField field)
        {
            return field.FieldType.Contains("StandardField");
        }

        public static bool IsValidStartField(MoveDestinationField field)
        {
            StartField startField = field as StartField;
            if(startField?.CurrentMeeple != null)
                return !startField.CurrentMeeple.IsStartFieldBlocked;
            return startField != null && startField.CurrentMeeple == null;
        }
        private static bool IsSimpleInvalidChangeField(MoveDestinationField field)
        {
            KennelField kennelField = field as KennelField;
            EndField endField = field as EndField;

            return kennelField != null || endField != null;
        }

        private static bool IsPartnerColorCode(ColorCode myColor, ColorCode partnerColor)
        {
            if ((myColor == ColorCode.Red && partnerColor == ColorCode.Yellow) ||
                (myColor == ColorCode.Yellow && partnerColor == ColorCode.Red))
                return true;
            return (myColor == ColorCode.Blue && partnerColor == ColorCode.Green) ||
                   (myColor == ColorCode.Green && partnerColor == ColorCode.Blue);
        }

        private static int GetDifferenceBetweenTwoFields(MoveDestinationField startField, MoveDestinationField endField)
        {
            int counter = 0;
            MoveDestinationField tempField = startField.Next;
            while (tempField != null)
            {
                if(!tempField.FieldType.Contains("EndField"))
                    counter++;
                if (tempField.Identifier == endField.Identifier)
                    return counter;
                tempField = tempField.Next;
            }
            return -1;
        }

        public static MoveDestinationField GetFieldById(GameTable actualTable, int fieldId)
        {
            MoveDestinationField moveDestinationField = null;
            PlayerFieldArea playerFieldArea = actualTable.PlayerFieldAreas.Find(area => area.Fields.Find(field =>
            {
                if (field.Identifier != fieldId)
                    return false;
                moveDestinationField = field;
                return true;
            }) != null);

            if (moveDestinationField == null)
            {
                PlayerFieldArea playerFieldAreaKennel = actualTable.PlayerFieldAreas.Find(area => area.KennelFields.Find(field =>
                {
                    if (field.Identifier != fieldId) return false;
                    moveDestinationField = field;
                    return true;
                }) != null);
            }
            return moveDestinationField;
        }

        public static bool ValidateMove(MeepleMove meepleMove, CardMove cardMove)
        {
            Meeple movedMeeple = meepleMove.Meeple;
            MoveDestinationField destinationField = meepleMove.MoveDestination;
            int test = 0;

            if (movedMeeple == null || cardMove == null)
                return false;
            foreach (CardAttribute attribute in cardMove.Card.Attributes)
            {
                switch (attribute.Attribute)
                {
                    case CardFeature.ChangePlace:
                        if (ProveChangePlace(movedMeeple, destinationField))
                        {
                            test += 1;
                            if((cardMove.Card.Name == "cardJoker" && GetDifferenceBetweenTwoFields(movedMeeple.CurrentPosition, destinationField) > 13)||
                                (cardMove.Card.Name == "cardJoker" && IsPartnerColorCode(movedMeeple.ColorCode, destinationField.CurrentMeeple.ColorCode) && GetDifferenceBetweenTwoFields(movedMeeple.CurrentPosition, destinationField) <= 13) ||
                                (cardMove.Card.Name != "cardJoker"))
                                cardMove.SelectedAttribute = new CardAttribute(CardFeature.ChangePlace);
                        }
                        break;
                    case CardFeature.LeaveKennel:
                        if (ProveLeaveKennel(movedMeeple, destinationField))
                            test += 1;
                        break;
                }

                if (ProveValueCard(movedMeeple, destinationField, (int) attribute.Attribute))
                    test += 1;
            }
            return test > 0;
        }

        public static bool CanMoveToEndFields(MoveDestinationField startCountField, int fieldDistanceCount, ColorCode meepleColorCode)
        {
            if (HasBlockedField(startCountField, fieldDistanceCount))
                return false;
            int fieldCount = fieldDistanceCount;
            if (fieldDistanceCount > 0)
            {
                for (int i = 0; i <= fieldDistanceCount; i++)
                {
                    if (startCountField == null)
                        return false;
                    startCountField = startCountField.Next;
                    fieldCount--;
                    StartField startField = startCountField as StartField;
                    if (startField == null)
                        continue;
                    EndField endField = startField.EndFieldEntry;
                    fieldCount--;
                    for (int j = fieldCount; j >= 0; j--)
                    {
                        endField = (EndField)endField.Next;
                        if (endField == null)
                            return false;
                    }
                    return startField.ColorCode == meepleColorCode;
                }
            }

            else
            {
                for (int i = 0; i > fieldDistanceCount; i--)
                {
                    if (startCountField == null)
                        return false;
                    startCountField = startCountField.Previous;
                    fieldCount++;
                    StartField startField = startCountField as StartField;
                    if (startField == null)
                        continue;
                    EndField endField = startField.EndFieldEntry;
                    fieldCount++;
                    for (int j = fieldCount; j < 0; j++)
                    {
                        endField = (EndField)endField.Next;
                        if (endField == null)
                            return false;
                    }
                    return startField.ColorCode == meepleColorCode;
                }
            }
            
            return false;
        }

        public static bool HasBlockedField(MoveDestinationField startCountField, int fieldCount)
        {
            if (fieldCount < 0)
            {
                if (startCountField.FieldType.Contains("StartField"))
                {
                    startCountField = startCountField.Previous;
                    fieldCount++;
                }
                for (int i = 0; i > fieldCount; i--)
                {
                    if (startCountField == null)
                        return false;
                    while (startCountField.FieldType.Contains("EndField"))
                    {
                        startCountField = startCountField.Previous;
                        if (startCountField == null)
                            return fieldCount == i;
                    }
                        
                    StartField startField = startCountField as StartField;
                    if (startField != null)
                    {
                        return startField.CurrentMeeple != null && startField.CurrentMeeple.IsStartFieldBlocked;
                    }

                    startCountField = startCountField.Previous;
                }
                return false;
            }

            else
            {
                if (startCountField.FieldType.Contains("StartField"))
                {
                    startCountField = startCountField.Next;
                    fieldCount--;
                }
                
                for (int i = 0; i <= fieldCount; i++)
                {
                    if (startCountField == null)
                        return false;

                    while (startCountField.FieldType.Contains("EndField"))
                    {
                        startCountField = startCountField.Next;
                        if (startCountField == null)
                            return fieldCount == i;
                    }
                        
                    StartField startField = startCountField as StartField;
                    if (startField != null)
                    {
                        return startField.CurrentMeeple != null && startField.CurrentMeeple.IsStartFieldBlocked;
                    }
                    startCountField = startCountField.Next;
                }
                return false;
            }
        }

        public static MoveDestinationField GetNextField(MoveDestinationField currentPos, int value)
        {
            if (currentPos.FieldType.Contains("KennelField"))
                return null;
            if (value > 0)
            {
                while (value > 0)
                {
                    currentPos = currentPos.Next;
                    if (!IsSimpleInvalidChangeField(currentPos))
                        --value;
                }
            }
            else
            {
                while (value < 0)
                {
                    currentPos = currentPos.Previous;
                    if (!IsSimpleInvalidChangeField(currentPos))
                        value++;
                }
            }
            return currentPos;
        }
    }
}