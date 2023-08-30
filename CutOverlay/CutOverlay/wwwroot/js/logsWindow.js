const socketURL = "ws://localhost:37110/";
let retryInterval = 5000; // Initial retry interval in milliseconds

const logContent = document.getElementById("logPanelContent");
const maxLogs = 1000; // Maximum number of logs to keep
let logCount = 0;

function getLogLevel(logText) {
    if (logText.startsWith("[Trace]: ")) {
        return "log-trace";
    } else if (logText.startsWith("[Debug]: ")) {
        return "log-debug";
    } else if (logText.startsWith("[Information]: ")) {
        return "log-info";
    } else if (logText.startsWith("[Warning]: ")) {
        return "log-warn";
    } else if (logText.startsWith("[Error]: ")) {
        return "log-error";
    } else if (logText.startsWith("[Critical]: ")) {
        return "log-critical";
    } else {
        return "log-info"; // Use a default style for logs without recognized levels
    }
}

function connectLoggerWebSocket() {
    const socket = new WebSocket(socketURL);

    socket.onopen = event => {
        console.log("Connected to WebSocket");
    };

    socket.onmessage = event => {
        if (logCount >= maxLogs) {
            // Remove the oldest log before adding a new one
            logContent.removeChild(logContent.firstChild);
        } else {
            logCount++;
        }

        const log = document.createElement("div");
        log.innerText = event.data;
        const logLevel = getLogLevel(event.data);
        log.classList.add(logLevel); // Add a class for styling
        logContent.appendChild(log);
        logContent.scrollTop = logContent.scrollHeight;
    };

    socket.onclose = event => {
        console.log("WebSocket closed:", event);
        setTimeout(connectLoggerWebSocket, retryInterval);
    };

    socket.onerror = error => {
        console.error("WebSocket error:", error);
    };
}

connectLoggerWebSocket(); // Start the initial connection