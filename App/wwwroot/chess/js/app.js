
document.addEventListener('DOMContentLoaded', () => {
    // Ensure chess.js and Chessboard are available
    if (typeof Chess !== 'function' && typeof Chess !== 'object') {
        console.error('chess.js not loaded (expects global Chess).');
        return;
    }
    if (typeof Chessboard === 'undefined') {
        console.error('chessboard.js not loaded (expects global Chessboard).');
        return;
    }

    // engine + board
    const game = new Chess();   // chess.js engine
    let board = null;

    // UI elements
    const statusEl = document.getElementById('status');
    const fenEl = document.getElementById('fen');
    const movesEl = document.getElementById('moves');
    const resetBtn = document.getElementById('resetBtn');
    const flipBtn = document.getElementById('flipBtn');
    const undoBtn = document.getElementById('undoBtn');

    
    // Called when the user drops a piece
    function onDrop(source, target) {
        // try the move on the engine
        const move = game.move({
            from: source,
            to: target,
            promotion: 'q' // auto-queen for simplicity
        });

        // Illegal move: snap back
        if (move === null) return 'snapback';

        // legal move; update UI
        updateUI();
    }

    // After piece snap animation, ensure board position matches engine
    function onSnapEnd() {
        board.position(game.fen());
    }

    function updateUI() {
        // Status: whose move / check / checkmate etc.
        let status = '';
        const turn = game.turn() === 'w' ? 'White' : 'Black';

        if (game.in_checkmate()) {
            status = 'Checkmate — ' + (turn === 'White' ? 'Black' : 'White') + ' wins';
        } else if (game.in_draw() || game.in_stalemate() || game.insufficient_material()) {
            status = 'Draw';
        } else {
            status = `${turn} to move`;
            if (game.in_check()) status += ' — Check!';
        }

        statusEl.textContent = status;

        // FEN
        fenEl.value = game.fen();

        // Moves list
        const hist = game.history({ verbose: false }); // short algebraic moves
        movesEl.textContent = hist.join(' ');
    }

    // Initialize chessboard.js with callbacks
    const config = {
        draggable: true,
        position: 'start',
        onDrop: onDrop,
        onSnapEnd: onSnapEnd,
        // Updated pieceTheme to point to the new chess folder
        pieceTheme: '/chess/img/chesspieces/wikipedia/{piece}.png'
    };

    // Create board
    board = Chessboard('board', config);

    // Buttons
    resetBtn.addEventListener('click', () => {
        game.reset();
        board.start();
        updateUI();
    });

    flipBtn.addEventListener('click', () => {
        board.flip();
    });

    undoBtn.addEventListener('click', () => {
        const move = game.undo();
        if (move !== null) {
            board.position(game.fen());
            updateUI();
        }
    });

    // initial UI
    updateUI();

    // Expose a tiny debug API (optional)
    window.__game = game;
    window.__board = board;
});
