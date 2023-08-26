const MAX_MESSAGES = 20; // Maximum number of messages to display
const MESSAGE_EXPIRATION_TIME = 15000; // Message expiration time in milliseconds
var removingMessages = [];

function addMessage(username, message, userColor, flags, userBadges, userProfileImageUrl, paint) {
    const chatContainers = document.getElementsByClassName("chat-messages");

    for (let i = 0; i < chatContainers.length; i++) {
        const messageElement = document.createElement("div");
        messageElement.classList.add("message");
        let profileElement = document.createElement("span");
        profileElement.classList.add("badge");
        profileElement.classList.add("overlayMessage");
        profileElement.style.content = "url(\"" + userProfileImageUrl + "\")";
        profileElement.style.width = "38px";
        profileElement.style.height = "38px";
        profileElement.style.margin = "2px 12px 2px 0";
        messageElement.appendChild(profileElement);
        const messageInnerElement = document.createElement("div");
        if (flags.highlighted) {
            messageInnerElement.classList.add("highlighted");
            messageInnerElement.classList.add("overlayMessage");
            messageInnerElement.style.borderColor = userColor;
            var rgb = hexToRgb(userColor);
            messageInnerElement.style.background =
                `linear-gradient(-60deg, rgba(${rgb.r},${rgb.g},${rgb.b},0.5) 0%, rgba(${rgb.r},${rgb.g},${rgb.b
                },0.2) 100%)`;
        }

        for (let j = 0; j < userBadges.length; j++) {
            let badgeElement = document.createElement("span");
            badgeElement.classList.add("badge");
            badgeElement.classList.add("overlayMessage");
            badgeElement.style.content = "url(\"" + userBadges[j] + "\")";
            messageInnerElement.appendChild(badgeElement);
        }

        messageElement.setAttribute("data-time", Date.now());

        let usernameElement = document.createElement("span");
        usernameElement.classList.add("chatUser");
        usernameElement.classList.add("overlayMessage");
        if (paint == null || paint.backgroundImage == null) {
            usernameElement.style.background = `linear-gradient(-60deg, ${userColor} -50%, #ffffff 200%)`;
            usernameElement.style.webkitTextFillColor = "transparent";
            usernameElement.style.webkitBackgroundClip = "text";
        } else {
            usernameElement.style.backgroundColor = paint.backgroundColor;
            usernameElement.style.backgroundImage = paint.backgroundImage;
            usernameElement.style.backgroundSize = paint.backgroundSize;
            usernameElement.style.filter = paint.filter;
            usernameElement.style.webkitTextFillColor = "transparent";
            usernameElement.style.webkitBackgroundClip = "text";
        }
        usernameElement.textContent = `${username}`;
        messageInnerElement.appendChild(usernameElement);

        let messageTextElement = document.createElement("span");
        messageTextElement.classList.add("overlayMessage");
        messageTextElement.innerHTML = twemoji.parse(message);
        messageTextElement.style.background = "linear-gradient(-60deg, #9b9b9b 0%, #ffffff 50%)";
        messageTextElement.style.webkitTextFillColor = "transparent";
        messageTextElement.style.webkitBackgroundClip = "text";
        messageInnerElement.appendChild(messageTextElement);

        messageElement.appendChild(messageInnerElement);
        chatContainers[i].appendChild(messageElement);

        // Scroll to the bottom of the chat container
        chatContainers[i].scrollTop = chatContainers[i].scrollHeight;
    }
}

function removeExpiredMessages() {
    const chatContainers = document.getElementsByClassName("chat-messages");

    // Remove expired messages
    const currentTime = Date.now();
    for (let i = 0; i < chatContainers.length; i++) {
        const container = chatContainers[i];
        const messages = container.getElementsByClassName("message");
        for (let i = 0; i < messages.length; i++) {
            const messageTime = parseInt(messages[i].getAttribute("data-time"), 10);
            if (currentTime - messageTime > MESSAGE_EXPIRATION_TIME) {
                if (removingMessages.length > 0 && removingMessages.includes(messages[i])) {
                    continue;
                }
                removingMessages.push(messages[i]);
                messages[i].style.animationName = "slide-up";
                messages[i].addEventListener("animationend",
                    function() {
                        // Remove message from index
                        removingMessages.splice(removingMessages.indexOf(messages[i]), 1);
                        container.removeChild(messages[i]);
                    });
            }
        }
    }
}

const socketURL = 'ws://localhost:37101/';
let retryInterval = 5000; // Initial retry interval in milliseconds

function connectWebSocket() {
    const socket = new WebSocket(socketURL);

    socket.onopen = event => {
        console.log('Connected to WebSocket');
    };

    socket.onmessage = event => {
        console.log(event.data);
        const data = JSON.parse(event.data);
        const displayName = data.displayName;
        const message = data.message;
        const emotes = data.messageEmotes || {};
        const userColor = data.userColor;
        const flags = data.flags;
        const userBadges = data.userBadges;
        const userProfileImageUrl = data.userProfileImageUrl;
        const paint = data.paint;
        // Replace emote codes with emote images
        var messageWithEmotes = replaceEmotes(message, emotes);
        addMessage(displayName, messageWithEmotes, userColor, flags, userBadges, userProfileImageUrl, paint);
    };

    socket.onclose = event => {
        console.log('WebSocket closed:', event);
        fetch("/twitch/apiConnection", { method: "GET" });
        setTimeout(connectWebSocket, retryInterval);
    };

    socket.onerror = error => {
        console.error('WebSocket error:', error);
    };
}

connectWebSocket(); // Start the initial connection

function replaceEmotes(message, emotes) {
    var emoteList = [];

    emotes.forEach(function (emoteData) {
        emoteList.push([emoteData.url, [emoteData.startIndex, emoteData.endIndex]]);
    });

    // Sort emote list by position in descending order
    emoteList.sort(function(a, b) {
        return b[1][0] - a[1][0];
    });

    var output = "";

    for (let i = 0; i < message.length; i++) {
        let emoji = false;
        // Check if theres any emojis in this index point, if there is then add it to the output and skip to the end of the range
        for (let j = 0; j < emoteList.length; j++) {
            const [url, [start, end]] = emoteList[j];
            if (i === start) {
                const emoteImage =
                    `<img src="${url}" alt="" />`;
                output += emoteImage;
                i = end;
                emoji = true;
                break;
            }
        }

        if (emoji) {
            continue;
        }

        const char = message[i];
        output += char;
    }

    return output;
}

try {
    const response = fetch("/configuration", { method: "GET" });
    response.then((res) => {
        if (res.ok) {
            res.json().then((configurations) => {
                if (configurations["twitchChat"] === "true") {
                    ComfyJS.Init(configurations["twitchUsername"]);
                }
            });
        } else {
            console.error("Failed to fetch configuration");
        }
    });
} catch (error) {
    console.error("An error occurred while fetching the configuration");
}

// Run the removeExpiredMessages function every second (100 milliseconds)
setInterval(removeExpiredMessages, 1000);

$(document).ready(checkUpdate);