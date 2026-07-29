
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
}

function onSnapEnd() {
    board.position(game.fen());
}

function updateStatus() {
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
    movesEl.textContent = game.history().join(' ');
}

function sendMove(move) {
    const moveMsg = {
        type: 'Move',
        Move: {
            From: move.from,
            To: move.to,
            Promotion: move.promotion ? move.promotion : null,
            StateAfter: game.fen()
        }
    };
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
    const wsUrl = `${protocol}//${window.location.host}/chess/ws/${gameId}?side=${playerColor}`;
    socket = new WebSocket(wsUrl);

    socket.onmessage = (event) => {
        const msg = JSON.parse(event.data);
        console.log('Received:', msg);

        if (msg.type === 'StartGame') {
            game.load(msg.InitialFen);
            board.position(game.fen());
            whiteTimeMs = parseTimeSpan(msg.WhiteTime);
            blackTimeMs = parseTimeSpan(msg.BlackTime);
            activeColor = game.turn() === 'w' ? 'white' : 'black';
            updateStatus();
        } else if (msg.type === 'RequestMove') {
            game.load(msg.Fen);
            board.position(game.fen());
            whiteTimeMs = parseTimeSpan(msg.WhiteTime);
            blackTimeMs = parseTimeSpan(msg.BlackTime);
            activeColor = game.turn() === 'w' ? 'white' : 'black';
            updateStatus();
        } else if (msg.type === 'EndGame') {
            activeColor = null;
            alert(`Game Over: ${msg.Result} ${msg.Reason || ''}`);
        }
    };

    socket.onclose = () => {
        console.log('Socket closed');
        activeColor = null;
    };
}

function parseTimeSpan(ts) {
    if (!ts) return 0;
    // .NET TimeSpan format: "00:05:00" or "00:05:00.123"
    const parts = ts.split(':');
    const hours = parseInt(parts[0]);
    const minutes = parseInt(parts[1]);
    const secondsParts = parts[2].split('.');
    const seconds = parseInt(secondsParts[0]);
    const ms = secondsParts[1] ? parseInt(secondsParts[1].padEnd(3, '0').substring(0, 3)) : 0;
    
    return (((hours * 60 + minutes) * 60 + seconds) * 1000) + ms;
}

document.getElementById('resetBtn').addEventListener('click', () => {
    window.location.href = '/chess/selection.html';
});

document.getElementById('flipBtn').addEventListener('click', () => {
    board.flip();
});

initGame();
