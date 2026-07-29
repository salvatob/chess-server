# WebSocket Communication Protocol

## Overview

The WebSocket Communication Protocol defines the messages exchanged between the server and a remote human player during a chess game.

The protocol acts as the transport layer between the server-side `SocketPlayer` implementation and the frontend client. It is independent of the underlying WebSocket implementation and specifies only the semantics of the exchanged messages.

The protocol follows a simple command-based design inspired by the Universal Chess Interface (UCI). Unlike UCI, however, it is intended for communication with a graphical client representing a human player rather than another chess engine.

All messages are serialized as JSON and represented internally by strongly typed DTOs.

All time values are represented as simple discrete values in milliseconds.

---

# Design Goals

The protocol is designed to:

- remain simple and easy to understand,
- clearly separate transport from game logic,
- provide strongly typed messages instead of string parsing,
- allow future protocol extensions

Communication is asynchronous.

---

## Server → Client Messages

| Message | Description | Payload |
|---------|-------------|---------|
| `StartGame` | Indicates that a game has started. | `Color` (string), `InitialFen` (string), `WhiteTime` (TimeSpan), `BlackTime` (TimeSpan), `Increment` (TimeSpan) |
| `RequestMove` | Requests the player's next move. | `Fen` (string), `WhiteTime` (TimeSpan), `BlackTime` (TimeSpan) |
| `EndGame` | Indicates that the game has ended. | `Outcome` (int/enum), `Reason` (string?) |

---

## Client → Server Messages

| Message | Description | Payload |
|---------|-------------|---------|
| `Move` | Submits the player's chosen move. | The selected move. |

---

## Responsibilities

The server is the authority over the game and drives the protocol by issuing commands to the client.

The client is responsible for responding to these commands and presenting the game state to the user.
