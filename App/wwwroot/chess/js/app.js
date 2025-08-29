// app.js - Exposes a small global API wrapper around chess.js + chessboard.js
// Place in /chess/js/app.js and load after vendor libs in index.html

(function (global) {
    // -------------------------
    // Compatibility helpers
    // -------------------------
    function hasFn(obj, name) {
        return typeof obj?.[name] === 'function';
    }

    function _isCheckmate(game) {
        if (hasFn(game, 'isCheckmate')) return game.isCheckmate();
        if (hasFn(game, 'in_checkmate')) return game.in_checkmate();
        if (hasFn(game, 'moves') && hasFn(game, 'in_check')) {
            return game.moves().length === 0 && game.in_check();
        }
        return false;
    }

    function _isDraw(game) {
        if (hasFn(game, 'isDraw')) return game.isDraw();
        if (hasFn(game, 'in_draw')) return game.in_draw();
        if (hasFn(game, 'isStalemate') && game.isStalemate()) return true;
        if (hasFn(game, 'in_stalemate') && game.in_stalemate()) return true;
        if (hasFn(game, 'isInsufficientMaterial') && game.isInsufficientMaterial()) return true;
        return false;
    }

    function _isStalemate(game) {
        if (hasFn(game, 'isStalemate')) return game.isStalemate();
        if (hasFn(game, 'in_stalemate')) return game.in_stalemate();
        if (hasFn(game, 'moves') && hasFn(game, 'in_check')) {
            return game.moves().length === 0 && !game.in_check();
        }
        return false;
    }

    // -------------------------
    // Internal state
    // -------------------------
    let _game = null;        // chess.js engine instance
    let _board = null;       // chessboard.js instance
    let _config = {};        // config used at init
    let _onMoveHandlers = []; // callbacks: (moveObj, fen) => void

    // Safe accessor helpers
    function _ensureEngine() {
        if (!_game) _game = new Chess();
        return _game;
    }

    // -------------------------
    // UI callbacks for chessboard.js
    // -------------------------
    function _onDrop(source, target, piece, newPos, oldPos, orientation) {
        // try the move on engine, auto-queen if promotion needed (can be extended)
        const game = _ensureEngine();

        const move = game.move({ from: source, to: target, promotion: 'q' });

        if (move === null) {
            return 'snapback'; // illegal
        }

        // notify handlers
        _emitMove(move, game.fen());
        // update status UI if desired (external code can call getFEN/getStatus)
        return undefined;
    }

    function _onSnapEnd() {
        if (_board && _game && hasFn(_game, 'fen')) {
            _board.position(_game.fen());
        }
    }

    // -------------------------
    // Event emitter
    // -------------------------
    function _emitMove(moveObj, fen) {
        _onMoveHandlers.forEach(fn => {
            try { fn(moveObj, fen); } catch (e) { console.error('ChessAPI:onMove handler error', e); }
        });
    }

    // -------------------------
    // Public API
    // -------------------------
    const ChessAPI = {
        /**
         * init(options)
         * options:
         *  - elementId (string): id of the container element (default: 'board')
         *  - pieceTheme (string): URL template for pieces, e.g. '/chess/img/chesspieces/wikipedia/{piece}.png'
         *  - draggable (bool): enable drag (default true)
         *  - position (string|object): starting position, default 'start'
         *  - autoInit (bool): if true, init will run on DOMContentLoaded automatically (default false)
         */
        init(options = {}) {
            _config = Object.assign({
                elementId: 'board',
                pieceTheme: '/chess/img/chesspieces/wikipedia/{piece}.png',
                draggable: true,
                position: 'start',
                autoInit: false
            }, options);

            // if DOM is ready, create now; otherwise wait if autoInit true
            if (document.readyState === 'complete' || document.readyState === 'interactive') {
                _createBoard();
            } else if (_config.autoInit) {
                document.addEventListener('DOMContentLoaded', _createBoard);
            }
        },

        /**
         * create the board (internal)
         */
        _createBoard: _createBoard,

        /**
         * reset() - reset engine and board to starting position
         */
        reset() {
            _ensureEngine().reset();
            if (_board && hasFn(_board, 'start')) _board.start();
            return { ok: true, fen: _game ? _game.fen() : null };
        },

        /**
         * move(from, to, {promotion}) - applies the move (returns move object or null)
         */
        move(from, to, opts = {}) {
            const g = _ensureEngine();
            const move = g.move({ from, to, promotion: opts.promotion || 'q' });
            if (move && _board) _board.position(g.fen());
            if (move) _emitMove(move, g.fen());
            return move;
        },

        /**
         * validateMove(from, to, {promotion}) - returns true if legal
         */
        validateMove(from, to, opts = {}) {
            const g = _ensureEngine();
            // Use legal moves to check
            if (!hasFn(g, 'moves')) return false;
            const moves = g.moves({ verbose: true });
            return moves.some(m => m.from === from && m.to === to && (!opts.promotion || m.promotion === opts.promotion));
        },

        /**
         * generateMoves(square) - returns array of SAN (or verbose if verbose=true)
         * If square is provided, returns moves from that square.
         */
        generateMoves(square = null, verbose = false) {
            const g = _ensureEngine();
            if (square) return g.moves({ square, verbose });
            return g.moves({ verbose });
        },

        generateFuturePositions() {
            // if (!engine) engine = new Chess();

            const tempEngine = new Chess(_game.fen()); // clone current state
            const moves = tempEngine.moves({ verbose: true });
            const result = [];

            for (const m of moves) {
                tempEngine.move(m);                  // apply move
                result.push({ move: m, fen: tempEngine.fen() });
                tempEngine.undo();                   // revert
            }

            return result;
        },


        /**
         * getFEN() / setFEN(fen)
         */
        getFEN() {
            const g = _ensureEngine();
            return hasFn(g, 'fen') ? g.fen() : null;
        },

        setFEN(fen) {
            const g = _ensureEngine();
            // Some engines throw on invalid fen; wrap in try
            try {
                if (hasFn(g, 'load')) {
                    g.load(fen);
                } else if (hasFn(g, 'load')) {
                    g.load(fen);
                } else if (hasFn(g, 'fen')) {
                    // older api fallback: try to load via constructor (not ideal)
                    // not implemented
                    console.warn('setFEN: engine missing load() method for this build');
                }
                if (_board) _board.position(g.fen());
                return { ok: true, fen: g.fen() };
            } catch (e) {
                return { ok: false, error: e?.message || String(e) };
            }
        },

        /**
         * getHistory() - returns moves history (array of SAN strings)
         */
        getHistory() {
            const g = _ensureEngine();
            if (hasFn(g, 'history')) return g.history();
            if (hasFn(g, 'moves')) return g.moves();
            return [];
        },

        /**
         * undo() - undo last move (returns move object or null)
         */
        undo() {
            const g = _ensureEngine();
            const m = hasFn(g, 'undo') ? g.undo() : null;
            if (m && _board) _board.position(g.fen());
            return m;
        },

        /**
         * flip() - flip board orientation
         */
        flip() {
            if (_board && hasFn(_board, 'flip')) _board.flip();
        },

        /**
         * getTurn() - 'w' or 'b'
         */
        getTurn() {
            const g = _ensureEngine();
            return hasFn(g, 'turn') ? g.turn() : null;
        },

        /**
         * status helpers: isCheck, isCheckmate, isDraw...
         */
        isCheck() {
            const g = _ensureEngine();
            return hasFn(g, 'in_check') ? g.in_check() : (hasFn(g, 'isCheck') ? g.isCheck() : false);
        },

        isCheckmate() {
            return _isCheckmate(_ensureEngine());
        },

        isDraw() {
            return _isDraw(_ensureEngine());
        },

        isStalemate() {
            return _isStalemate(_ensureEngine());
        },

        /**
         * register an onMove handler: fn(moveObj, fen)
         * returns a function to unsubscribe
         */
        onMove(fn) {
            if (typeof fn !== 'function') throw new Error('onMove handler must be a function');
            _onMoveHandlers.push(fn);
            return () => {
                const idx = _onMoveHandlers.indexOf(fn);
                if (idx >= 0) _onMoveHandlers.splice(idx, 1);
            };
        },

        /**
         * direct access (for debugging). Prefer API methods.
         */
        _internal: {
            getEngine: () => _game,
            getBoard: () => _board,
            getConfig: () => _config
        }
    };

    // -------------------------
    // board creation function
    // -------------------------
    function _createBoard() {
        // Only create once
        if (_board) return;

        // Ensure libs are present
        if (typeof Chess !== 'function' && typeof Chess !== 'object') {
            console.error('Chess engine (chess.js) not loaded.');
            return;
        }
        if (typeof Chessboard === 'undefined') {
            console.error('Chessboard UI (chessboard.js) not loaded.');
            return;
        }

        // create engine
        _game = new Chess();

        // board config (respect provided options)
        const bcfg = {
            draggable: !!_config.draggable,
            position: _config.position || 'start',
            onDrop: _onDrop,
            onSnapEnd: _onSnapEnd,
            pieceTheme: _config.pieceTheme
        };

        _board = Chessboard(_config.elementId, bcfg);
    }

    // attach public API to global
    global.ChessAPI = ChessAPI;

    // convenient auto-init if someone calls ChessAPI.init({autoInit:true}) earlier
    // (we still require explicit init for normal usage)
    // If init was called before DOM ready with autoInit true, we handled it in init()
    // Otherwise, do nothing.

})(window);

ChessAPI.init()