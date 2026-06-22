// chess-api.js

class ChessGame {
    constructor(boardElementId, fenElementId, movesElementId) {
        this.game = new Chess();
        this.board = Chessboard(boardElementId, {
            draggable: true,
            position: 'start',
            onDrop: this.onDrop.bind(this)
        });
        this.fenElement = document.getElementById(fenElementId);
        this.movesElement = document.getElementById(movesElementId);
        this.updateStatus();
    }

    updateBoard() {
        this.board.position(this.game.fen());
    }
    
    onDrop(source, target) {
        let move = this.game.move({ from: source, to: target, promotion: 'q' });
        if (move === null) return 'snapback';
        // this.updateStatus();
        updateUI()
    }

    undo() {
        this.game.undo();
        this.board.position(this.game.fen());
        this.updateStatus();
    }

    reset() {
        this.game.reset();
        this.board.start();
        this.updateStatus();
    }

    flip() {
        this.board.flip();
    }

    updateStatus() {
        if (this.fenElement) this.fenElement.textContent = this.game.fen();
        if (this.movesElement) this.movesElement.textContent = this.game.history().join(', ');
    }

    generateMovesWithFEN() {
        let moves = this.game.moves({ verbose: true });
        return moves.map(m => {
            let tempGame = new Chess(this.game.fen());
            tempGame.move(m.san);
            return { move: m.san, fen: tempGame.fen() };
        });
    }
}

class ChessClock {
    constructor(initialMs = 1000 * 60 * 5) {
        this.whiteMs = initialMs;
        this.blackMs = initialMs;
        this.interval = null;
        this.active = null;
    }

    start(color) {
        this.stop();
        const timeBetween = 100;
        this.active = color;
        this.interval = setInterval(() => {
            if (this.active === 'white') this.whiteMs -= timeBetween;
            if (this.active === 'black') this.blackMs -= timeBetween;
        }, timeBetween);
    }

    stop() {
        if (this.interval) clearInterval(this.interval);
        this.interval = null;
        this.active = null;
    }

    setTime(color, seconds) {
        if (color === 'white') this.whiteMs = seconds;
        if (color === 'black') this.blackMs = seconds;
    }

    getTimes() {
        return { white: this.whiteMs, black: this.blackMs };
    }
}

class ChessAPIClass {
    constructor() {
        this.game = new ChessGame('board', 'fen', 'moves');
        this.clock = new ChessClock();
        this.id = null
        this.requestId().catch(e => console.error(e))
        document.getElementById('resetBtn').addEventListener('click', () => this.game.reset());
        document.getElementById('flipBtn').addEventListener('click', () => this.game.flip());
        document.getElementById('undoBtn').addEventListener('click', () => this.game.undo());
    }

    async requestId() {
        this.id = await requestNewChessGame()
    }
    
    makeMove(from, to, promotion = 'q') {
        let move = this.game.game.move({ from, to, promotion });
        if (move) this.game.updateBoard();
        this.game.updateStatus();
        // updateUI();
        return move;
    }
    
    getFEN() { return this.game.game.fen(); }
    getMoves() { return this.game.game.history(); }
    generateMovesWithFEN() { return this.game.generateMovesWithFEN(); }
    
    getMoveDTO() {
        return new moveDTO(
            this.id,
            this.clock.whiteMs,
            this.clock.blackMs,
            this.getFEN()
        )
    }
    
    moveFromDTO(moveDTO) {
        const possibleMoves = this.generateMovesWithFEN()
        const moveSan = possibleMoves.find(m=>m.fen === moveDTO.stateAfter)
        if (!moveSan) {
            console.error("Move is unavailable")
            return
        }
        
        this.game.game.move(moveSan)
        
    }
}

// Singleton instance
const ChessAPI = new ChessAPIClass();
window.ChessAPI = ChessAPI;
// app.js


// Initialize Chess API singleton
const api = ChessAPI;

// Update move list and FEN on each move
function updateUI() {
    const moves = api.getMoves();
    const moveList = document.getElementById('moves');
    // moveList.innerHTML = '';
    moveList.innerHTML = moves.join(" ")
    // moves.forEach((m, i) => {
    //     const li = document.createElement('li');
    //     li.textContent = `${i + 1}. ${m}`;
    //     moveList.appendChild(li);
    // });

    document.getElementById('fen').textContent = api.getFEN();
}

// Wrap ChessGame methods to update UI automatically
['makeMove','move','resetBoard','flipBoard','undo'].forEach(fn => {
    const orig = api[fn];
    api[fn] = function(...args){
        const res = orig.apply(api, args);
        updateUI();
        return res;
    };
});


// Render clocks in DOM every 250ms
function renderClocks(){
    const clock = api.clock;
    
    const whiteEl = document.getElementById('whiteClock');
    const blackEl = document.getElementById('blackClock');
    
    if(whiteEl) whiteEl.textContent = formatMs(clock.whiteMs);
    if(blackEl) blackEl.textContent = formatMs(clock.blackMs);
}

function formatMs(ms){
    const totalSec = Math.floor(ms / 1000);
    const min = Math.floor(totalSec / 60);
    const sec = totalSec % 60;
    return `${min.toString().padStart(2,'0')}:${sec.toString().padStart(2,'0')}`;
}

setInterval(renderClocks, 250);

// Initial render
renderClocks();
