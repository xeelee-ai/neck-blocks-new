using Tomino.Input;
using Tomino.Model;
using Tomino.Shared;
using UnityEngine;

namespace Tomino.View
{
    public class BoardView : MonoBehaviour
    {
        private enum Layer
        {
            Blocks,
            PieceShadow
        }

        public GameObject blockPrefab;
        public Sprite blockSprite;
        public ThemeProvider themeProvider;
        public Sprite shadowBlockSprite;
        public readonly TouchInput touchInput = new();

        private Board _gameBoard;
        private int _renderedBoardHash = -1;
        private bool _forceRender;
        private GameObjectPool<BlockView> _blockViewPool;
        private RectTransform _rectTransform;

        public void SetBoard(Board board)
        {
            _gameBoard = board;
            var size = board.width * board.height + 10;
            _blockViewPool = new GameObjectPool<BlockView>(blockPrefab, size, gameObject);
        }

        private void RenderGameBoard()
        {
            _blockViewPool.DeactivateAll();
            RenderPieceShadow();
            RenderBlocks();
        }

        private void RenderBlocks()
        {
            foreach (var block in _gameBoard.Blocks)
            {
                RenderBlock(block);
            }
        }

        private void RenderPieceShadow()
        {
            foreach (var position in _gameBoard.GetPieceShadow())
            {
                var view = _blockViewPool.GetAndActivate();
                view.SetSprite(shadowBlockSprite);
                view.SetSize(BlockSize());
                view.SetColor(themeProvider.currentTheme.blockShadowColor);
                view.SetPosition(BlockPosition(position.Row, position.Column, Layer.PieceShadow));
            }
        }

        private void RenderBlock(Block block)
        {
            var view = _blockViewPool.GetAndActivate();
            UpdateBlockView(block, view);
            view.SetPosition(BlockPosition(block.Position.Row, block.Position.Column, Layer.Blocks));
        }

        internal void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            Settings.changedEvent += () => _forceRender = true;
        }

        internal void Update()
        {
            touchInput.blockSize = BlockSize();

            var hash = _gameBoard.GetHashCode();
            if (!_forceRender && hash == _renderedBoardHash) return;

            RenderGameBoard();
            _renderedBoardHash = hash;
            _forceRender = false;
        }

        internal void OnRectTransformDimensionsChange()
        {
            _forceRender = true;
        }

        private Vector3 BlockPosition(int row, int column, Layer layer)
        {
            var size = BlockSize();
            var position = new Vector3(column * size, row * size, (float)layer);
            var offset = new Vector3(size / 2, size / 2, 0);
            return position + offset - PivotOffset();
        }

        private float BlockSize()
        {
            var boardWidth = _rectTransform.rect.size.x;
            return boardWidth / _gameBoard.width;
        }

        private Vector3 PivotOffset()
        {
            var pivot = _rectTransform.pivot;
            var boardSize = _rectTransform.rect.size;
            return new Vector3(boardSize.x * pivot.x, boardSize.y * pivot.y);
        }

        private void UpdateBlockView(Block block, BlockView blockView)
        {
            blockView.SetSprite(blockSprite);
            blockView.SetSize(BlockSize());
            blockView.SetColor(block.Color);
            blockView.SetPosition(BlockPosition(block.Position.Row, block.Position.Column, Layer.Blocks));
        }
    }
}
