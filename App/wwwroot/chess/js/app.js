
let game = new Chess();
let board = null;
let socket = null;
let playerColor = 'white';
let gameId = null;

let whiteClockEl;
let blackClockEl;
let statusEl;
let fenEl;
let movesEl;

function initUI() {
    whiteClockEl = document.getElementById('whiteClock');
    blackClockEl = document.getElementById('blackClock');
    statusEl = document.getElementById('status');
    fenEl = document.getElementById('fen');
    movesEl = document.getElementById('moves');
    setupPromotionUI();
}

let whiteTimeMs = 0;
let blackTimeMs = 0;
let lastTick = Date.now();
let activeColor = null;
let pendingMove = null;

let promotionModal;
let promotionButtons;

function setupPromotionUI() {
    promotionModal = document.getElementById('promotion-modal');
    promotionButtons = document.querySelectorAll('.promotion-options button');

    promotionButtons.forEach(button => {
        button.addEventListener('click', () => {
            const promotion = button.getAttribute('data-promotion');
            completeMove(promotion);
        });
    });

    window.addEventListener('click', (event) => {
        if (event.target === promotionModal) {
            promotionModal.style.display = 'none';
            pendingMove = null;
            board.position(game.fen());
        }
    });
}

function completeMove(promotionPiece) {
    if (promotionModal) promotionModal.style.display = 'none';
    if (!pendingMove) return;

    const move = game.move({
        from: pendingMove.from,
        to: pendingMove.to,
        promotion: promotionPiece
    });

    pendingMove = null;

    if (move === null) {
        board.position(game.fen());
        return 'snapback';
    }

    // Update internal activeColor before sending to avoid clock flickers
    activeColor = game.turn() === 'w' ? 'white' : 'black';
    updateStatus();
    sendMove(move);
    board.position(game.fen());
}


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

    if (whiteClockEl) whiteClockEl.textContent = formatTime(whiteTimeMs);
    if (blackClockEl) blackClockEl.textContent = formatTime(blackTimeMs);
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
    // Check if move is a promotion
    const piece = game.get(source);
    if (piece && piece.type === 'p') {
        const isWhite = piece.color === 'w';
        const targetRank = target[1];
        if ((isWhite && targetRank === '8') || (!isWhite && targetRank === '1')) {
            // Check if move is legal first (ignoring promotion piece for a moment)
            const moves = game.moves({ square: source, verbose: true });
            const isLegal = moves.some(m => m.to === target);
            
            if (isLegal) {
                pendingMove = { from: source, to: target };
                // Delay showing the modal slightly to let the piece land on the board visually
                setTimeout(() => {
                    if (promotionModal) {
                        promotionModal.style.display = 'block';
                    } else {
                        console.error('Promotion modal not found');
                        // Fallback: promote to queen if modal is missing
                        completeMove('q');
                    }
                }, 50);
                return; // Wait for user choice
            }
        }
    }

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
    if (pendingMove) return;
    board.position(game.fen());
}

function updateStatus() {
    if (!statusEl) return;
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
    if (fenEl) fenEl.textContent = game.fen();
    
    // Build move history text
    const history = game.history();
    let movesText = '';
    for (let i = 0; i < history.length; i += 2) {
        movesText += `${Math.floor(i / 2) + 1}. ${history[i]} ${history[i + 1] || ''} `;
    }
    if (movesEl) {
        movesEl.textContent = movesText.trim();
        movesEl.scrollTop = movesEl.scrollHeight;
    }
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
    initUI();
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
        if (statusEl) statusEl.classList.remove('game-over');
    };

    socket.onmessage = (event) => {
        if (pendingMove) {
            // If we receive a message while choosing promotion, 
            // it's likely out of sync or a late update. 
            // We should probably handle it, but for now let's just log.
            console.log('Received socket message while promotion is pending');
        }
        console.log('Raw message data:', event.data);
        const msg = JSON.parse(event.data);
        console.log('Parsed message:', msg);

        if (msg.type === 'StartGame' || msg.type === 'PrepareGame') {
            if (statusEl) statusEl.classList.remove('game-over');
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
            if (msg.type === 'PrepareGame') {
                activeColor = null; // Clocks not started yet
                if (statusEl) statusEl.textContent = 'Preparing game...';
            }
            updateStatus();
        } else if (msg.type === 'GameStarted') {
            activeColor = game.turn() === 'w' ? 'white' : 'black';
            lastTick = Date.now();
            updateStatus();
        } else if (msg.type === 'OpponentMove') {
            const moveLAN = msg.MoveLAN || msg.moveLAN;
            const move = game.move(moveLAN, { sloppy: true });
            if (!move) {
                console.warn('Failed to apply opponent move:', moveLAN, 'Current FEN:', game.fen(), 'Expected FEN:', msg.FenAfter || msg.fenAfter);
                // Last resort: sync FEN if move application failed
                if (game.fen() !== (msg.FenAfter || msg.fenAfter)) {
                    game.load(msg.FenAfter || msg.fenAfter);
                }
            }
            board.position(game.fen());
            activeColor = game.turn() === 'w' ? 'white' : 'black';
            updateStatus();
        } else if (msg.type === 'RequestMove') {
            const lastMove = msg.LastMoveLAN || msg.lastMoveLAN;
            if (lastMove) {
                const move = game.move(lastMove, { sloppy: true });
                if (!move) {
                    console.warn('Failed to apply last move:', lastMove, 'Current FEN:', game.fen(), 'Expected FEN:', msg.Fen || msg.fen);
                }
            }
            
            // Sync FEN only if we are out of sync, but try to avoid game.load() if possible
            // because game.load() clears move history.
            const targetFen = msg.Fen || msg.fen;
            if (game.fen() !== targetFen) {
                console.log('FEN out of sync, updating...');
                game.load(targetFen);
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
            
            if (statusEl) {
                statusEl.innerHTML = status;
                statusEl.classList.add('game-over');
            }
        } else if (msg.type === 'ErrorMessage') {
            const errorMsg = msg.Message || msg.message;
            const isGameEnd = msg.GameEnd !== undefined ? msg.GameEnd : msg.gameEnd;
            
            console.error('Server error:', errorMsg);
            
            if (isGameEnd) {
                if (statusEl) {
                    statusEl.innerHTML = `<span style="color: red;"><strong>Fatal Error:</strong> ${errorMsg}</span>`;
                    statusEl.classList.add('game-over');
                }
                activeColor = null;
            } else {
                // Temporary error notification could be better, but for now just alert/log
                alert('Error: ' + errorMsg);
            }
        }
    };

    socket.onclose = (event) => {
        if (event.wasClean && event.code === 1000) {
            console.log('WebSocket closing handshake ran successfully');
        } else {
            console.log('Socket closed', event.wasClean ? 'cleanly' : 'uncleanly', 'with code:', event.code);
        }
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
