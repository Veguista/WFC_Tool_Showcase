using UnityEngine;

namespace WFC
{
    namespace Directions
    {
        // WARNING: Don't asign values to the directions and don't change the order they are currently in.
        //          If you wish to add new directions, place them after the already existing directions.
        //          Also, DON'T ASSIGN VALUES OVER 255 (a byte).
        //          Ignoring these warnings may result in unpredictable behavior and (probably) mayhem.
        public enum Direction
        {
            right, left,
            up, down,
            hex_right_up, hex_left_up,
            hex_right_down, hex_left_down,
            above, below,
            diagonal_right_up, diagonal_left_up,
            diagonal_right_down, diagonal_left_down
        }

        public static class DirectionFunctions
        {
            public static Direction ReturnOppositeDirection(Direction originalDirection)
            {
                switch (originalDirection)      // Organized in opposite pairs.
                {
                    case Direction.right:
                        return Direction.left;
                    case Direction.left:
                        return Direction.right;

                    case Direction.up:
                        return Direction.down;
                    case Direction.down:
                        return Direction.up;

                    case Direction.hex_right_up:
                        return Direction.hex_left_down;
                    case Direction.hex_left_down:
                        return Direction.hex_right_up;

                    case Direction.hex_left_up:
                        return Direction.hex_right_down;
                    case Direction.hex_right_down:
                        return Direction.hex_left_up;

                    case Direction.above:
                        return Direction.below;
                    case Direction.below:
                        return Direction.above;

                    case Direction.diagonal_right_up:
                        return Direction.diagonal_left_down;
                    case Direction.diagonal_left_down:
                        return Direction.diagonal_right_up;

                    case Direction.diagonal_left_up:
                        return Direction.diagonal_right_down;
                    case Direction.diagonal_right_down:
                        return Direction.diagonal_left_up;


                    default:
                        Debug.LogError("Direction Opposite not defined.");
                        return Direction.right;
                }
            }
        }
    }
}