﻿var followersSince = "";
var shown = false;

const RGBToHSL = (r, g, b) => {
    r /= 255;
    g /= 255;
    b /= 255;
    const l = Math.max(r, g, b);
    const s = l - Math.min(r, g, b);
    const h = s
        ? l === r
        ? (g - b) / s
        : l === g
        ? 2 + (b - r) / s
        : 4 + (r - g) / s
        : 0;
    return [
        60 * h < 0 ? 60 * h + 360 : 60 * h,
        100 * (s ? (l <= 0.5 ? s / (2 * l - s) : s / (2 - (2 * l - s))) : 0),
        (100 * (2 * l - s)) / 2,
    ];
};

function hexToRgb(hex) {
    const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
    return result
        ? {
            r: parseInt(result[1], 16),
            g: parseInt(result[2], 16),
            b: parseInt(result[3], 16)
        }
        : null;
}

function checkUpdate() {
    // Hide heart rate text if empty
    const heartRateText = document.getElementById("heartRateText");
    if (heartRateText.innerText === "") {
        heartRateText.parentElement.parentElement.style.display = "none";
    } else {
        heartRateText.parentElement.parentElement.style.display = null;
    }
}

setInterval(checkUpdate, 2000);

function fetchFollows() {
    try {
        const response = fetch(`/twitch/followers?since=${followersSince}`, { method: "GET" });
        response.then((res) => {
            if (res.ok) {
                res.json().then((data) => {
                    const followers = data.Followers;
                    followersSince = data.FetchTime;

                    if (followers.length > 0) {
                        for (let i = followers.length - 1; i >= 0; i--) {
                            alertMessage(followers[i] + " has joined the foxxos!");
                        }
                    }
                });
            } else {
                console.error("An error occurred while fetching new followers");
            }
        });
    } catch (error) {
        console.error("An error occurred while fetching new followers");
    }
    setTimeout(() => {
            fetchFollows();
        },
        3000);
}

fetchFollows();

function fetchLatestFollowSub() {
    const lastFollowerElement = document.getElementById("lastFollowText");

    if (lastFollowerElement != null) {
        try {
            const followResponse = fetch("/twitch/latest/follow", { method: "GET" });
            followResponse.then((res) => {
                if (res.ok) {
                    res.json().then((follow) => {
                        if (follow == null || follow === "") {
                            lastFollowerElement.innerText = "";
                            lastFollowerElement.parentElement.parentElement.style.display = "none";
                        } else {
                            lastFollowerElement.innerText = follow.name;
                            lastFollowerElement.parentElement.parentElement.style.display = null;
                        }
                    });
                } else {
                    lastFollowerElement.innerText = "";
                    lastFollowerElement.parentElement.parentElement.style.display = "none";
                }
            });
        } catch (error) {
            console.error("An error occurred while fetching new followers");
        }
    }

    const lastSubElement = document.getElementById("lastSubText");

    if (lastSubElement != null) {
        try {
            const response = fetch("/twitch/latest/sub", { method: "GET" });
            response.then((res) => {
                if (res.ok) {
                    res.json().then((sub) => {
                        if (sub == null || sub === "") {
                            lastSubElement.innerText = "";
                            lastSubElement.parentElement.parentElement.style.display = "none";
                        } else {
                            lastSubElement.innerText = sub.name;
                            lastSubElement.parentElement.parentElement.style.display = null;
                        }
                    });
                } else {
                    lastSubElement.innerText = "";
                    lastSubElement.parentElement.parentElement.style.display = "none";
                }
            });
        } catch (error) {
            console.error("An error occurred while fetching new followers");
        }
    }

    setTimeout(() => {
            fetchLatestFollowSub();
        },
        3000);
}

fetchLatestFollowSub();

var alerts = [];

function alertLoop() {
    try {
        if (alerts.length > 0) {
            const message = alerts[0];
            showAlertMessage(message);
        }
    } catch (ex) {
        // ignored
    }

    let removed = false;
    if (alerts.length > 0) {
        removed = true;
        alerts.shift();
    }

    if (removed) {
        setTimeout(() => {
                alertLoop();
            },
            4500);
    } else {
        setTimeout(() => {
                alertLoop();
            },
            500);
    }
}

alertLoop();

function alertMessage(message) {
    alerts.push(message);
}

function showAlertMessage(message) {
    const statusDiv = document.getElementById("statusDiv");
    const panelItems = document.getElementsByClassName("panelItem");
    const messagePanel = document.getElementById("panel-message-content");
    const messageContentDiv = document.getElementById("panelMessageContentSpanDiv");
    const messageContent = document.getElementById("panelMessageContentSpan");

    messageContent.textContent = message;

    messagePanel.style.display = "block";
    messagePanel.style.opacity = "1";

    // Slide in the message
    messageContentDiv.style.animation = "slideInAlertMessage 0.5s ease-out forwards";

    for (let i = 0; i < panelItems.length; i++) {
        panelItems[i].style.opacity = "0";
    }
    statusDiv.style.opacity = "0";

    setTimeout(() => {
            // Slide out the message
            messageContentDiv.style.left = "100%";
            messageContentDiv.style.animation = "slideOutAlertMessage 0.5s ease-in forwards";

            // Fade out the panel
            messagePanel.style.opacity = "0";

            setTimeout(() => {
                    // Reset styles and hide the panel
                    messagePanel.style.display = "none";
                    messageContentDiv.style.left = "-100%";
                    messageContentDiv.style.animation = "";

                    for (let i = 0; i < panelItems.length; i++) {
                        panelItems[i].style.opacity = "1";
                    }
                    statusDiv.style.opacity = "1";
                },
                500);
        },
        3500); // Stay for 3 seconds + slide in animation duration
}

setInterval(() => {
        const heartRateElement = document.getElementById("heartRateText");

        if (heartRateElement == null)
            return;

        try {
            const response = fetch("/pulsoid/status", { method: "GET" });
            response.then((res) => {
                if (res.ok) {
                    res.text((status) => {
                        if (status == null || status === "") {
                            heartRateElement.innerText = "";
                        } else {
                            heartRateElement.innerText = status;
                        }
                    });
                } else {
                    heartRateElement.innerText = "";
                }
            });
        } catch (error) {
            console.error("An error occurred while fetching Spotify status");
            heartRateElement.innerText = "";
        }
    },
    5000);