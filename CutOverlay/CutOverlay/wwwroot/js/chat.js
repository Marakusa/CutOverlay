const MAX_MESSAGES = 20; // Maximum number of messages to display
const MESSAGE_EXPIRATION_TIME = 15000; // Message expiration time in milliseconds
var removingMessages = [];

function addMessage(username, message, userColor, flags, extra) {
    const chatContainers = document.getElementsByClassName("chat-messages");

    for (let i = 0; i < chatContainers.length; i++) {
        let messageElement = document.createElement("div");
        messageElement.classList.add("message");
        if (flags.highlighted) {
            messageElement.classList.add("highlighted");
            messageElement.style.borderColor = userColor;
            var rgb = hexToRgb(userColor);
            messageElement.style.background =
                `linear-gradient(-60deg, rgba(${rgb.r},${rgb.g},${rgb.b},0.5) 0%, rgba(${rgb.r},${rgb.g},${rgb.b
                },0.2) 100%)`;
        }
        if (flags.broadcaster) {
            let badgeElement = document.createElement("span");
            badgeElement.classList.add("badge");
            badgeElement.classList.add("broadcaster");
            messageElement.appendChild(badgeElement);
        }
        if (flags.mod) {
            let badgeElement = document.createElement("span");
            badgeElement.classList.add("badge");
            badgeElement.classList.add("moderator");
            messageElement.appendChild(badgeElement);
        }
        if (flags.vip) {
            let badgeElement = document.createElement("span");
            badgeElement.classList.add("badge");
            badgeElement.classList.add("vip");
            messageElement.appendChild(badgeElement);
        }
        if (flags.subscriber) {
            let badgeElement = document.createElement("span");
            badgeElement.classList.add("badge");
            badgeElement.classList.add("subscriber");
            messageElement.appendChild(badgeElement);
        }
        if (flags.founder) {
            let badgeElement = document.createElement("span");
            badgeElement.classList.add("badge");
            badgeElement.classList.add("founder");
            messageElement.appendChild(badgeElement);
        }
        messageElement.setAttribute("data-time", Date.now());

        let usernameElement = document.createElement("span");
        usernameElement.classList.add("chatUser");
        usernameElement.style.background = `linear-gradient(-60deg, ${userColor} -50%, #ffffff 200%)`;
        usernameElement.style.webkitTextFillColor = "transparent";
        usernameElement.style.webkitBackgroundClip = "text";
        usernameElement.textContent = `${username}`;
        messageElement.appendChild(usernameElement);

        let messageTextElement = document.createElement("span");
        messageTextElement.innerHTML = twemoji.parse(message);
        messageTextElement.style.background = "linear-gradient(-60deg, #9b9b9b 0%, #ffffff 50%)";
        messageTextElement.style.webkitTextFillColor = "transparent";
        messageTextElement.style.webkitBackgroundClip = "text";
        messageElement.appendChild(messageTextElement);

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

ComfyJS.onChat = (user, message, flags, self, extra) => {
    // Replace emote codes with emote images
    var emotes = extra.messageEmotes || {};
    var messageWithEmotes = replaceEmotes(message, emotes);
    addMessage(extra.displayName, messageWithEmotes, extra.userColor, flags, extra);
};

function replaceEmotes(message, emotes) {
    var emoteList = [];
    const emoteCodes = Object.keys(emotes);

    emoteCodes.forEach(function(emoteCode) {
        emotes[emoteCode].forEach(function(emotePosition) {
            emoteList.push([emoteCode, emotePosition.split("-")]);
        });
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
            const [emoteCode, [start, end]] = emoteList[j];
            if (i == start) {
                const emoteImage =
                    `<img src="https://static-cdn.jtvnw.net/emoticons/v2/${emoteCode}/default/dark/1.0" alt="${emoteCode
                        }" />`;
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
