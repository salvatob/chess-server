class moveDTO {
    constructor(id, whiteMs, blackMs, fenBefore) {
        this.gameId = id
        this.whiteMs = whiteMs
        this.blackMs = blackMs
        this.fenBefore = fenBefore
        this.fenAfter = null
    }
}

async function requestNewChessGame() {
    let newId = await fetch("/chess/new-game")
    if (!newId.ok) {
        console.error('New game request has failed');
        return
    }
    return await newId.json()
    
}

async function requestNextMove(moveDTO) {
    let move = await fetch("/chess/move", {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(moveDTO)
        })
    
    if (!move.ok) {
        console.error('Move request has failed');
        return
    }
    
    return await move.json()
} 