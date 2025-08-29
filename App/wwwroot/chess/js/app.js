// ChessGame: handles board + chess.js rules
class ChessGame {
    constructor(boardElementId) {
        this.game = new Chess();
        this.board = Chessboard(boardElementId, {
            draggable: true,
            position: 'start',
            onDrop: this.onDrop.bind(this)
        });
    }

    onDrop(source, target) {
        const move = this.game.move({ from: source, to: target, promotion: 'q' });
        if (move === null) return 'snapback';
        this.board.position(this.game.fen());
    }

    reset() {
        this.game.reset();
        this.board.start();
    }

    flip() {
        this.board.flip();
    }

    move(from, to, promotion = 'q') {
        const move = this.game.move({ from, to, promotion });
        if (move) this.board.position(this.game.fen());
        return move;
    }

    getFen() {
        return this.game.fen();
    }

    getMoves(options = {}) {
        return this.game.moves(options);
    }

    getNextFens() {
        const moves = this.game.moves({ verbose: true });
        return moves.map(m => {
            const copy = new Chess(this.game.fen());
            copy.move(m);
            return copy.fen();
        });
    }
}

// ChessClock: manages two player clocks
class ChessClock {
    constructor(initialTimeMs = 5 * 60 * 1000, incrementMs = 2000) {
        this.time = [initialTimeMs, initialTimeMs];
        this.increment = incrementMs;
        this.running = [false, false];
        this.lastTick = null;
        this.active = 0; // 0 = white, 1 = black
    }

    start(player) {
        this.stop();
        this.active = player;
        this.running[player] = true;
        this.lastTick = Date.now();
    }

    stop() {
        if (this.running[this.active]) {
            this.update();
            this.running[this.active] = false;
        }
    }

    switchTurn() {
        this.stop();
        this.time[this.active] += this.increment;
        this.start(1 - this.active);
    }

    update() {
        if (!this.running[this.active]) return;
        const now = Date.now();
        const diff = now - this.lastTick;
        this.time[this.active] -= diff;
        this.lastTick = now;
    }

    getTime(player) {
        this.update();
        return this.time[player];
    }

    setTime(player, ms) {
        this.time[player] = ms;
    }
}

// ChessAPI singleton
class ChessAPIClass {
    constructor() {
        this.game = new ChessGame('board');
        this.clock = new ChessClock();

        // Connect UI buttons (if present)
        const resetBtn = document.getElementById('resetBtn');
        if (resetBtn) resetBtn.addEventListener('click', () => this.game.reset());

        const flipBtn = document.getElementById('flipBtn');
        if (flipBtn) flipBtn.addEventListener('click', () => this.game.flip());
    }

    // ChessGame methods
    move(from, to, promotion) { return this.game.move(from, to, promotion); }
    getFen() { return this.game.getFen(); }
    getMoves(options) { return this.game.getMoves(options); }
    getNextFens() { return this.game.getNextFens(); }

    resetBoard() { return this.game.reset(); }
    flipBoard() { return this.game.flip(); }

    // ChessClock methods
    startClock(player) { return this.clock.start(player); }
    stopClock() { return this.clock.stop(); }
    switchTurn() { return this.clock.switchTurn(); }
    getTime(player) { return this.clock.getTime(player); }
    setTime(player, ms) { return this.clock.setTime(player, ms); }
}

// Singleton assignment
theChessAPI = new ChessAPIClass();
window.ChessAPI = theChessAPI;
