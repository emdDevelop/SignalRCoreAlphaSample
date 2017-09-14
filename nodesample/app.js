var signalr = require('@aspnet/signalr-client');

// Polyfill (kinda hacky for now)
XMLHttpRequest = require('xmlhttprequest').XMLHttpRequest;
WebSocket = require('websocket').w3cwebsocket;

var connection = new signalr.HubConnection('http://localhost:5000/chat');

connection.on('send', data => {
    console.log(`Received: ${data}`);
});

process.stdin.addListener('data', data => {
    connection.invoke('send', data.toString('utf8'));
});

connection.start();
