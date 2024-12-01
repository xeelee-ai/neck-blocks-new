using System.Collections.Generic;
using System.Linq;
using Tomino.Shared;
using UnityEngine;

namespace Tomino.Model
{
    /// <summary>
    /// Contains collection of blocks placed on the board and allows for moving them within the
    /// defined bounds.
    /// </summary>
    public class Board
    {
        /// <summary>
        /// The width of the board.
        /// </summary>
        public readonly int width;

        /// <summary>
        /// The height of the board.
        /// </summary>
        public readonly int height;

        /// <summary>
        /// The collection of blocks placed on the board.
        /// </summary>
        public List<Block> Blocks { get; } = new();

        /// <summary>
        /// The current falling piece.
        /// </summary>
        /// <value></value>
        public Piece Piece { get; private set; }

        /// <summary>
        /// The piece that will be added to the board when the current piece finishes falling.
        /// </summary>
        /// <returns></returns>
        public Piece NextPiece => _pieceProvider.GetNextPiece();

        private readonly IPieceProvider _pieceProvider;
        private int Top => height - 1;

        /// <summary>
        /// Initializes board with specified size and a `BalancedPieceProvider`.
        /// </summary>
        /// <param name="width">The width of the board.</param>
        /// <param name="height">The height of the board.</param>
        public Board(int width, int height) : this(width, height, new BalancedRandomPieceProvider())
        {
        }

        /// <summary>
        /// Initializes board with specified size and piece provider.
        /// </summary>
        /// <param name="width">The width of the board.</param>
        /// <param name="height">The height of the board.</param>
        /// <param name="pieceProvider">The piece provider.</param>
        public Board(int width, int height, IPieceProvider pieceProvider)
        {
            this.width = width;
            this.height = height;
            _pieceProvider = pieceProvider;
        }

        /// <summary>
        /// Determines whether blocks on the board collide with board bounds or with themselves.
        /// </summary>
        /// <returns>true if collisions were detected; false otherwise.</returns>
        public bool HasCollisions()
        {
            return HasBoardCollisions() || HasBlockCollisions();
        }

        private bool HasBlockCollisions()
        {
            var allPositions = Blocks.Map(block => block.Position);
            var uniquePositions = new HashSet<Position>(allPositions);
            return allPositions.Length != uniquePositions.Count;
        }

        private bool HasBoardCollisions()
        {
            return Blocks.Find(CollidesWithBoard) != null;
        }

        private bool CollidesWithBoard(Block block)
        {
            return block.Position.Row < 0 ||
                   block.Position.Row >= height ||
                   block.Position.Column < 0 ||
                   block.Position.Column >= width;
        }

        public override int GetHashCode()
        {
            return (from block in Blocks
                let row = block.Position.Row
                let column = block.Position.Column
                let offset = width * height * (int)block.Type
                select offset + row * width + column).Sum();
        }

        /// <summary>
        /// Adds new piece.
        /// </summary>
        public void AddPiece()
        {
            // 获取新的 Piece
            Piece = _pieceProvider.GetPiece();
            // 设置当前方块引用
            CurrentPiece = Piece;
            // 设置随机颜色
            CurrentPiece.SetColor(BlockColors.GetRandomColor());

            var offsetRow = Top - Piece.Top;
            var offsetCol = (width - Piece.Width) / 2;

            foreach (var block in Piece.blocks)
            {
                block.MoveBy(offsetRow, offsetCol);
            }

            Blocks.AddRange(Piece.blocks);
        }

        /// <summary>
        /// Returns position of the piece shadow which is the final piece position if it starts
        /// falling.
        /// </summary>
        /// <returns>Collection of piece blocks positions.</returns>
        public ICollection<Position> GetPieceShadow()
        {
            var positions = Piece.GetPositions();
            _ = FallPiece();
            var shadowPositions = Piece.GetPositions().Values.Map(p => p);
            RestoreSavedPiecePosition(positions);
            return shadowPositions;
        }

        /// <summary>
        /// Moves the current piece left by 1 column.
        /// </summary>
        public bool MovePieceLeft()
        {
            return MovePiece(0, -1);
        }

        /// <summary>
        /// Moves the current piece right by 1 column.
        /// </summary>
        public bool MovePieceRight()
        {
            return MovePiece(0, 1);
        }

        /// <summary>
        /// Moves the current piece down by 1 row.
        /// </summary>
        public bool MovePieceDown()
        {
            return MovePiece(-1, 0);
        }

        private bool MovePiece(int rowOffset, int columnOffset)
        {
            foreach (var block in Piece.blocks)
            {
                block.MoveBy(rowOffset, columnOffset);
            }

            if (!HasCollisions()) return true;

            foreach (var block in Piece.blocks)
            {
                block.MoveBy(-rowOffset, -columnOffset);
            }

            return false;
        }

        /// <summary>
        /// Rotates the current piece clockwise.
        /// </summary>
        public bool RotatePiece()
        {
            if (!Piece.canRotate)
            {
                return false;
            }

            var piecePosition = Piece.GetPositions();
            var offset = Piece.blocks[0].Position;

            foreach (var block in Piece.blocks)
            {
                var row = block.Position.Row - offset.Row;
                var column = block.Position.Column - offset.Column;
                block.MoveTo(-column + offset.Row, row + offset.Column);
            }

            if (!HasCollisions() || ResolveCollisionsAfterRotation()) return true;

            RestoreSavedPiecePosition(piecePosition);
            return false;
        }

        private bool ResolveCollisionsAfterRotation()
        {
            var columnOffsets = new[] { -1, -2, 1, 2 };
            foreach (var offset in columnOffsets)
            {
                _ = MovePiece(0, offset);

                if (HasCollisions())
                {
                    _ = MovePiece(0, -offset);
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        private void RestoreSavedPiecePosition(IReadOnlyDictionary<Block, Position> piecePosition)
        {
            foreach (var block in Piece.blocks)
            {
                block.MoveTo(piecePosition[block]);
            }
        }

        /// <summary>
        /// Immediately moves the current piece to the lowest possible row.
        /// </summary>
        /// <returns>Number of rows the piece has been moved down.</returns>
        public int FallPiece()
        {
            var rowsCount = 0;
            while (MovePieceDown())
            {
                rowsCount++;
            }

            return rowsCount;
        }

        /// <summary>
        /// Removes blocks in rows that hold maximum number of possible blocks (= board width). All
        /// blocks placed above the removed row are moved 1 row down.
        /// </summary>
        /// <returns></returns>
        public int RemoveFullRows()
        {
            var rowsRemoved = 0;
            for (var row = height - 1; row >= 0; --row)
            {
                var rowBlocks = GetBlocksFromRow(row);
                if (rowBlocks.Count != width) continue;

                Remove(rowBlocks);
                MoveDownBlocksBelowRow(row);
                rowsRemoved += 1;
            }

            return rowsRemoved;
        }

        /// <summary>
        /// Removes all blocks from the board.
        /// </summary>
        public void RemoveAllBlocks()
        {
            Blocks.Clear();
        }

        private List<Block> GetBlocksFromRow(int row)
        {
            return Blocks.FindAll(block => block.Position.Row == row);
        }

        private void Remove(ICollection<Block> blocksToRemove)
        {
            _ = Blocks.RemoveAll(blocksToRemove.Contains);
        }

        private void MoveDownBlocksBelowRow(int row)
        {
            foreach (var block in Blocks.Where(block => block.Position.Row > row))
            {
                block.MoveBy(-1, 0);
            }
        }

        public int RemoveMatchingColorBlocks()
        {
            var blocksRemoved = 0;
            var matchesToRemove = new HashSet<Position>();
            var blocksToKeep = new HashSet<Position>();  // 用于存储需要保留的方块位置

            // 检查水平方向的匹配
            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col <= width - 3; col++)
                {
                    var blocks = new List<Block>();
                    var positions = new List<Position>();
                    
                    // 收集连续的相同颜色方块
                    PieceType? currentType = null;  // 使用可空类型
                    var consecutiveCount = 0;
                    
                    for (int i = 0; i < 4; i++)  // 检查4个连续位置
                    {
                        if (col + i >= width) break;
                        
                        var block = GetBlock(new Position(row, col + i));
                        if (block == null) break;
                        
                        if (currentType == null)
                        {
                            currentType = block.Type;
                            consecutiveCount = 1;
                        }
                        else if (block.Type == currentType)
                        {
                            consecutiveCount++;
                        }
                        else
                        {
                            break;
                        }
                        
                        blocks.Add(block);
                        positions.Add(new Position(row, col + i));
                    }
                    
                    // 如果找到4个连续的相同颜色方块
                    if (consecutiveCount == 4)
                    {
                        // 随机保留一个方块
                        var keepIndex = UnityEngine.Random.Range(0, 4);
                        blocksToKeep.Add(positions[keepIndex]);
                        
                        // 将其他三个方块标记为要移除
                        for (int i = 0; i < 4; i++)
                        {
                            if (i != keepIndex)
                            {
                                matchesToRemove.Add(positions[i]);
                            }
                        }
                    }
                    // 如果正好是3个连续的相同颜色方块
                    else if (consecutiveCount == 3)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            matchesToRemove.Add(positions[i]);
                        }
                    }
                }
            }

            // 检查垂直方向的匹配（类似的逻辑）
            for (int col = 0; col < width; col++)
            {
                for (int row = 0; row <= height - 3; row++)
                {
                    var blocks = new List<Block>();
                    var positions = new List<Position>();
                    
                    PieceType? currentType = null;  // 使用可空类型
                    var consecutiveCount = 0;
                    
                    for (int i = 0; i < 4; i++)
                    {
                        if (row + i >= height) break;
                        
                        var block = GetBlock(new Position(row + i, col));
                        if (block == null) break;
                        
                        if (currentType == null)
                        {
                            currentType = block.Type;
                            consecutiveCount = 1;
                        }
                        else if (block.Type == currentType)
                        {
                            consecutiveCount++;
                        }
                        else
                        {
                            break;
                        }
                        
                        blocks.Add(block);
                        positions.Add(new Position(row + i, col));
                    }
                    
                    if (consecutiveCount == 4)
                    {
                        var keepIndex = UnityEngine.Random.Range(0, 4);
                        blocksToKeep.Add(positions[keepIndex]);
                        
                        for (int i = 0; i < 4; i++)
                        {
                            if (i != keepIndex)
                            {
                                matchesToRemove.Add(positions[i]);
                            }
                        }
                    }
                    else if (consecutiveCount == 3)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            matchesToRemove.Add(positions[i]);
                        }
                    }
                }
            }

            // 移除标记的方块，但保留需要保留的方块
            foreach (var position in matchesToRemove)
            {
                if (!blocksToKeep.Contains(position))
                {
                    RemoveBlock(position);
                    blocksRemoved++;
                }
            }

            // 如果有方块被移除，让上方的方块下落
            if (blocksRemoved > 0)
            {
                ApplyGravity();
            }

            return blocksRemoved;
        }

        private void ApplyGravity()
        {
            // 从底部开始，让方块下落填补空缺
            for (int col = 0; col < width; col++)
            {
                for (int row = 0; row < height - 1; row++)
                {
                    if (GetBlock(new Position(row, col)) == null)
                    {
                        // 找到上方最近的方块
                        for (int above = row + 1; above < height; above++)
                        {
                            var block = GetBlock(new Position(above, col));
                            if (block != null)
                            {
                                // 移动方块
                                RemoveBlock(new Position(above, col));
                                PlaceBlock(block, new Position(row, col));
                                break;
                            }
                        }
                    }
                }
            }
        }

        private Block GetBlock(Position position)
        {
            return Blocks.Find(block => block.Position.Equals(position));
        }

        private void RemoveBlock(Position position)
        {
            var block = GetBlock(position);
            if (block != null)
            {
                Blocks.Remove(block);
            }
        }

        private void PlaceBlock(Block block, Position position)
        {
            block.MoveTo(position);
            Blocks.Add(block);
        }

        // 添加 CurrentPiece 属性
        public Piece CurrentPiece { get; private set; }
    }
}
