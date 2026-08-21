
let game = new Chess();
let board = null;
let socket = null;
let playerColor = 'white';
let gameId = null;

const whiteClockEl = document.getElementById('whiteClock');
const blackClockEl = document.getElementById('blackClock');
const statusEl = document.getElementById('status');
const fenEl = document.getElementById('fen');
const movesEl = document.getElementById('moves');

let whiteTimeMs = 0;
let blackTimeMs = 0;
let lastTick = Date.now();
let activeColor = null;


function formatTime(ms) {
    if (ms < 0) ms = 0;
    const totalSec = Math.floor(ms / 1000);
    const min = Math.floor(totalSec / 60);
    const sec = totalSec % 60;
    return `${min.toString().padStart(2, '0')}:${sec.toString().padStart(2, '0')}`;
}

function updateClocks() {
    const now = Date.now();
    const delta = now - lastTick;
    lastTick = now;

    if (activeColor === null) return;

    if (activeColor === 'white') whiteTimeMs -= delta;
    if (activeColor === 'black') blackTimeMs -= delta;

    whiteClockEl.textContent = formatTime(whiteTimeMs);
    blackClockEl.textContent = formatTime(blackTimeMs);
}

setInterval(updateClocks, 100);

function onDragStart(source, piece, position, orientation) {
    if (game.game_over()) return false;
    if (activeColor !== playerColor) return false;
    if ((playerColor === 'white' && piece.search(/^b/) !== -1) ||
        (playerColor === 'black' && piece.search(/^w/) !== -1)) {
        return false;
    }
    
    // Check if it's actually the player's turn according to chess.js
    const turn = game.turn();
    if ((playerColor === 'white' && turn !== 'w') ||
        (playerColor === 'black' && turn !== 'b')) {
        return false;
    }
}

function onDrop(source, target) {
    const move = game.move({
        from: source,
        to: target,
        promotion: 'q'
    });

    if (move === null) return 'snapback';

    updateStatus();
    sendMove(move);
    activeColor = game.turn() === 'w' ? 'white' : 'black';
}

function onSnapEnd() {
    board.position(game.fen());
}

function updateStatus() {
    if (activeColor === null && statusEl.classList.contains('game-over')) {
        return; // Don't overwrite game over status
    }
    let status = '';

    let moveColor = 'White';
    if (game.turn() === 'b') {
        moveColor = 'Black';
    }

    if (game.in_checkmate()) {
        status = 'Game over, ' + moveColor + ' is in checkmate.';
    } else if (game.in_draw()) {
        status = 'Game over, drawn position';
    } else {
        status = moveColor + ' to move';
        if (game.in_check()) {
            status += ', ' + moveColor + ' is in check';
        }
    }

    statusEl.textContent = status;
    fenEl.textContent = game.fen();
    
    // Build move history text
    const history = game.history();
    let movesText = '';
    for (let i = 0; i < history.length; i += 2) {
        movesText += `${Math.floor(i / 2) + 1}. ${history[i]} ${history[i + 1] || ''} `;
    }
    movesEl.textContent = movesText.trim();
    movesEl.scrollTop = movesEl.scrollHeight;
}

function sendMove(move) {
    const moveMsg = {
        type: 'Move',
        move: {
            from: move.from,
            to: move.to,
            promotion: move.promotion ? move.promotion : null,
            stateAfter: game.fen()
        }
    };
    console.log('Sending move:', moveMsg);
    socket.send(JSON.stringify(moveMsg));
}

function initGame() {
    const urlParams = new URLSearchParams(window.location.search);
    gameId = urlParams.get('id');
    playerColor = urlParams.get('side') || 'white';

    if (!gameId) {
        window.location.href = '/chess/selection.html';
        return;
    }

    board = Chessboard('board', {
        draggable: true,
        position: 'start',
        orientation: playerColor,
        onDragStart: onDragStart,
        onDrop: onDrop,
        onSnapEnd: onSnapEnd
    });

    const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
    const isWhite = playerColor === 'white';
    const wsUrl = `${protocol}//${window.location.host}/chess/ws/${gameId}?whiteSide=${isWhite}`;
    socket = new WebSocket(wsUrl);

    socket.onopen = () => {
        console.log('WebSocket connection opened');
        statusEl.classList.remove('game-over');
    };

    socket.onmessage = (event) => {
        console.log('Raw message data:', event.data);
        const msg = JSON.parse(event.data);
        console.log('Parsed message:', msg);

        if (msg.type === 'StartGame') {
            statusEl.classList.remove('game-over');
            const initialFen = msg.InitialFen || msg.initialFen;
            if (initialFen) {
                game.load(initialFen);
            } else {
                game.reset();
            }
            board.position(game.fen());
            whiteTimeMs = msg.WhiteTimeMs || msg.whiteTimeMs || 0;
            blackTimeMs = msg.BlackTimeMs || msg.blackTimeMs || 0;
            
            // The API changed from Color: "white"/"black" to ColorWhite: true/false
            const colorWhite = msg.ColorWhite !== undefined ? msg.ColorWhite : msg.colorWhite;
            if (colorWhite !== undefined) {
                playerColor = colorWhite ? 'white' : 'black';
                board.orientation(playerColor);
            }
            
            activeColor = game.turn() === 'w' ? 'white' : 'black';
            updateStatus();
        } else if (msg.type === 'RequestMove') {
            const lastMove = msg.LastMoveLAN || msg.lastMoveLAN;
            if (lastMove) {
                const move = game.move(lastMove, { sloppy: true });
                if (!move) {
                    console.warn('Failed to apply last move:', lastMove, 'Falling back to game.load(FEN)');
                    game.load(msg.Fen || msg.fen);
                }
            } else {
                game.load(msg.Fen || msg.fen);
            }
            
            board.position(game.fen());
            whiteTimeMs = msg.WhiteTimeMs || msg.whiteTimeMs || 0;
            blackTimeMs = msg.BlackTimeMs || msg.blackTimeMs || 0;
            activeColor = game.turn() === 'w' ? 'white' : 'black';
            updateStatus();
        } else if (msg.type === 'EndGame') {
            activeColor = null;
            
            let resultText = 'Game Over';
            if (msg.Result !== undefined) {
                // GameResult enum mapping (common values)
                const results = {
                    0: 'White Wins',
                    1: 'Black Wins',
                    2: 'Draw',
                    'WhiteWins': 'White Wins',
                    'BlackWins': 'Black Wins',
                    'Draw': 'Draw'
                };
                resultText = results[msg.Result] || `Game Over: ${msg.Result}`;
            }
            
            let status = `<strong>${resultText}</strong>`;
            if (msg.Reason) {
                status += `<br/>${msg.Reason}`;
            } else if (msg.reason) {
                status += `<br/>${msg.reason}`;
            }
            
            statusEl.innerHTML = status;
            statusEl.classList.add('game-over');
        }
    };

    socket.onclose = () => {
        console.log('Socket closed');
        activeColor = null;
    };
}

document.getElementById('resetBtn').addEventListener('click', () => {
    window.location.href = '/chess/selection.html';
});

document.getElementById('flipBtn').addEventListener('click', () => {
    board.flip();
});

initGame();
