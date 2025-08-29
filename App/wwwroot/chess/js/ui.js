// ui.js - UI glue that wires ChessAPI to the DOM (move list, buttons, status, fen)
document.addEventListener('DOMContentLoaded', () => {
    // Initialize ChessAPI and create the board immediately
    ChessAPI.init({
        elementId: 'board',
        pieceTheme: '/chess/img/chesspieces/wikipedia/{piece}.png',
        draggable: true,
        position: 'start',
        autoInit: true
    });

    // DOM refs
    const statusEl = document.getElementById('status');
    const fenEl    = document.getElementById('fen');
    const movesEl  = document.getElementById('moves');
    const resetBtn = document.getElementById('resetBtn');
    const flipBtn  = document.getElementById('flipBtn');
    const undoBtn  = document.getElementById('undoBtn');

    function renderMoves() {
        const hist = ChessAPI.getHistory() || [];
        // join with spaces, but you can pretty-format later
        movesEl.textContent = hist.join(' ');
    }

    function updateStatusText() {
        const turn = ChessAPI.getTurn();
        let status = '';

        if (ChessAPI.isCheckmate()) {
            status = 'Checkmate — ' + (turn === 'w' ? 'Black' : 'White') + ' wins';
        } else if (ChessAPI.isStalemate()) {
            status = 'Stalemate';
        } else if (ChessAPI.isDraw()) {
            status = 'Draw';
        } else {
            status = (turn === 'w' ? 'White' : 'Black') + ' to move';
            if (ChessAPI.isCheck()) status += ' — Check!';
        }

        statusEl.textContent = status;
    }

    function updateUI() {
        updateStatusText();
        fenEl.value = ChessAPI.getFEN() || '';
        renderMoves();
    }

    // Button handlers
    if (resetBtn) {
        resetBtn.addEventListener('click', () => {
            ChessAPI.reset();
            updateUI();
        });
    }

    if (flipBtn) {
        flipBtn.addEventListener('click', () => {
            ChessAPI.flip();
            // flipping doesn't change FEN/moves, so no update required, but keep UI consistent
            // small delay if needed:
            setTimeout(updateUI, 0);
        });
    }

    if (undoBtn) {
        undoBtn.addEventListener('click', () => {
            const undone = ChessAPI.undo();
            if (undone !== null) updateUI();
        });
    }

    // Subscribe to move events from the API
    ChessAPI.onMove((moveObj, fen) => {
        // moveObj is the engine move object; fen is the new FEN
        // Immediately update UI
        updateUI();
    });

    // initial render
    updateUI();
});
