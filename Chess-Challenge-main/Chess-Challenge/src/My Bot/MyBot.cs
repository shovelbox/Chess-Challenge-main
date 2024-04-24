#define experimental
#define values

using ChessChallenge.API;
using System;
using System.Collections.Generic;

namespace ChessChallenge.Example
{
    public class MyBot : IChessBot
    {
        private const int MaxDepth = 64;
        private const int InfinityValue = 999999999;
        private static readonly int[] PieceValues = { 0, 100, 300, 310, 500, 900, 10000 };
        private readonly Random _randomizer = new();
        private readonly TranspositionTable _transpositionTable = new TranspositionTable(1000000);

        public Move Think(Board board, Timer timer)
        {
            var legalMoves = board.GetLegalMoves();
            var bestMove = Move.NullMove;
            int bestEval;

            if (timer.MillisecondsRemaining < 220)
            {
                bestMove = legalMoves[_randomizer.Next(legalMoves.Length)];
            }
            else
            {
                int color = board.IsWhiteToMove ? 1 : -1;
                bestEval = NegaMaxSearch(board, MaxDepth, color, -InfinityValue, InfinityValue, Move.NullMove, out bestMove);

#if values
                Console.WriteLine("{0}, eval: {1}", bestMove, bestEval);
#endif
            }

            return bestMove;
        }

        private int NegaMaxSearch(Board board, int depth, int color, int alpha, int beta, Move lastMove, out Move bestMove)
        {
            TranspositionTableEntry entry;
            if (_transpositionTable.TryGetValue(board.ZobristKey, out entry) && entry.Depth >= depth)
            {
                bestMove = Move.NullMove;
                return color * entry.Score;
            }

            if (board.IsDraw())
            {
                bestMove = Move.NullMove;
                return 0;
            }

            if (depth == 0 || board.GetLegalMoves().Length == 0)
            {
                bestMove = Move.NullMove;
                return color * EvaluatePosition(board, lastMove);
            }

            int maxEval = -InfinityValue;
            bestMove = Move.NullMove;

            var legalMoves = OrderMoves(board, board.GetLegalMoves());
            foreach (var move in legalMoves)
            {
                board.MakeMove(move);
                int evaluation = -NegaMaxSearch(board, depth - 1, -color, -beta, -alpha, move, out Move _);
                board.UndoMove(move);

                if (evaluation > maxEval)
                {
                    maxEval = evaluation;
                    bestMove = move;
                }

                alpha = Math.Max(alpha, evaluation);
                if (alpha >= beta)
                    break;
            }

            _transpositionTable[board.ZobristKey] = new TranspositionTableEntry(maxEval, depth);
            return maxEval;
        }

        private IList<Move> OrderMoves(Board board, IEnumerable<Move> moves)
        {
            var orderedMoves = new List<Move>();
            foreach (var move in moves)
            {
                if (IsCapture(board, move))
                {
                    orderedMoves.Insert(0, move);
                }
                else
                {
                    orderedMoves.Add(move);
                }
            }
            return orderedMoves;
        }

        private bool IsCapture(Board board, Move move)
        {
            var startPiece = board.GetPiece(new Square(move.StartSquare));
            var targetPiece = board.GetPiece(new Square(move.TargetSquare));

            return !targetPiece.IsNull && startPiece.IsWhite != targetPiece.IsWhite;
        }

        private int EvaluatePosition(Board board, Move lastMove)
        {
            int sum = 0;
            if (board.IsInCheckmate())
                return board.IsWhiteToMove ? -InfinityValue : InfinityValue;

            for (int i = 1; i < 7; i++)
                sum += (board.GetPieceList((PieceType)i, true).Count - board.GetPieceList((PieceType)i, false).Count) * PieceValues[i];

            if (board.IsInCheck())
                sum += board.IsWhiteToMove ? -800 : 800;

#if experimental
            if (board.SquareIsAttackedByOpponent(new Square(lastMove.StartSquare)))
                sum += 200;
#endif

            return sum;
        }

        private class TranspositionTable : Dictionary<ulong, TranspositionTableEntry>
        {
            public TranspositionTable(int capacity)
                : base(capacity)
            {
            }
        }

        private struct TranspositionTableEntry
        {
            public int Score;
            public int Depth;

            public TranspositionTableEntry(int score, int depth)
            {
                Score = score;
                Depth = depth;
            }
        }
    }
}