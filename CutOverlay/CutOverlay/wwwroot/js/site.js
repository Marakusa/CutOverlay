var newSong = "";
var newArtist = "";
var color = `0, 0, 0, 255`;
var shown = false;

// Set statusDiv hidden on left
const statusDiv = document.getElementById("statusDiv");
if (statusDiv != null)
    statusDiv.style.marginLeft = "-500px";

function hideText() {
    $("#artistNameDiv").animate({
            marginLeft: "-100px",
            opacity: 0
        },
        300);
    $("#songNameDiv").animate({
            marginLeft: "-100px",
            opacity: 0
        },
        300);

    document.getElementById("songNameDiv").classList.remove("scrolling");
    document.getElementById("artistNameDiv").classList.remove("scrolling");
}

function updateText() {
    document.getElementById("artist").innerHTML = newArtist;
    document.getElementById("artistShadow").innerHTML = newArtist;
    document.getElementById("song").innerHTML = newSong;
    document.getElementById("songShadow").innerHTML = newSong;
}

var progressData;

function showText() {
    $("#artistNameDiv").animate({
            marginLeft: "-4px",
            opacity: 1
        },
        300);
    $("#songNameDiv").animate({
            marginLeft: "85px",
            opacity: 1
        },
        300);
}

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

function checkUpdate() {
    // Hide heart rate text if empty
    const heartRateText = document.getElementById("heartRateText");
    if (heartRateText.innerText === "") {
        heartRateText.parentElement.parentElement.style.display = "none";
    } else {
        heartRateText.parentElement.parentElement.style.display = null;
    }

    // Hide song progress text if no song playing
    const progressText = document.getElementById("progressText");
    if (newSong === "") {
        progressText.parentElement.parentElement.style.display = "none";
    }

    try {
        const response = fetch("/spotify/status", { method: "GET" });
        response.then((res) => {
            if (res.ok) {
                res.json().then((status) => {

                    if (status == null || status.Status == null || status.Status.Paused) {
                        emptyConfig();
                    } else {
                        const artistsArray = status.Song.Artist.split(";").map(artist => artist.trim());
                        const uniqueArtistsArray = [...new Set(artistsArray)];
                        newArtist = uniqueArtistsArray.join("; ");
                        newSong = status.Song.Name;
                        color =
                            `${status.Song.Color.Red * 255}, ${status.Song.Color.Green * 255}, ${
                            status.Song.Color.Blue *
                            255}, 255`;
                        progressData = status.Status;
                    }
                    displayData();
                });
            } else {
                console.error("An error occurred while fetching Spotify status");
                emptyConfig();
                displayData();
            }
        });
    } catch (error) {
        console.error("An error occurred while fetching Spotify status");
        emptyConfig();
        displayData();
    }

    setTimeout(checkUpdate, 500);
}

function emptyConfig() {
    newArtist = "";
    newSong = "";
    color = `0, 0, 0, 255`;
    progressData = null;

    const progressText = document.getElementById("progressText");
    progressText.parentElement.parentElement.style.display = "none";
}

// Handle the music progress bar
setInterval(function () {
    if (progressData == null || newSong === "" || progressData.Progress === 0 || progressData.Total === 0)
        return;

    // Get the current timestamp
    const currentTime = Math.floor(Date.now() / 1000);

    // Calculate the elapsed time since FetchTime
    const elapsedSeconds = (progressData.Progress / 1000) + currentTime - progressData.FetchTime;
    const elapsedMilliseconds = progressData.Progress + Date.now() - (progressData.FetchTime * 1000);

    // Calculate the progress based on elapsed time
    var progressPercentage = ((elapsedSeconds * 1000.0) / progressData.Total);
    if (progressPercentage > 1.0)
        progressPercentage = 1.0;
    else if (progressPercentage < 0.0)
        progressPercentage = 0.0;

    // Convert progress percentage to seconds
    const adjustedProgress = Math.floor(progressPercentage * (progressData.Total / 1000));

    // Convert total to 0:00 format
    const totalMinutes = Math.floor(progressData.Total / 60000);
    const totalSeconds = Math.floor((progressData.Total - totalMinutes * 60000) / 1000);
    const totalString = totalMinutes + ":" + (totalSeconds < 10 ? `0${totalSeconds}` : totalSeconds);

    // Convert progress to 0:00 format
    const progressMinutes = Math.floor((adjustedProgress) / 60);
    const progressSeconds = Math.floor(((adjustedProgress) - progressMinutes * 60));
    const progressString = progressMinutes + ":" + (progressSeconds < 10 ? `0${progressSeconds}` : progressSeconds);

    const progressText = progressString + " / " + totalString;

    const progressBar = document.getElementById("progress");
    if (progressBar != null) {
        // Update the progress bar width
        progressBar.style.width = ((elapsedMilliseconds / progressData.Total) * 100) + "%";
    }

    // Update the progress text
    const progressTextElement = document.getElementById("progressText");
    progressTextElement.innerHTML = progressText;
    const progressTextShadowElement = document.getElementById("progressTextShadow");
    progressTextShadowElement.innerHTML = progressText;

    // Update the progress header text
    document.getElementById("progressHeader").innerHTML = "Track progress";
    document.getElementById("progressHeaderShadow").innerHTML = "Track progress";

    // Show progress text element
    progressTextElement.parentElement.parentElement.style.display = null;
},
    100);

function displayData() {
    if (newSong == null) newSong = "";
    if (newArtist == null) newArtist = "";
    if (color == null) color = `0, 0, 0, 255`;
    if ((newSong === "" && newSong !== document.getElementById("song").innerHTML) ||
        newSong !== document.getElementById("song").innerHTML) {
        if (newSong.length > 1 && !shown) {
            $("#statusDiv").animate({
                    marginLeft: "0px",
                },
                500);
            shown = true;
        }
        if (newSong.length < 1 && shown) {
            $("#statusDiv").animate({
                    marginLeft: "-500px",
                },
                500);
            shown = false;
        }

        hideText();
        setTimeout(updateText, 300);
        setTimeout(showText, 400);

        var imgPath = `/spotify/image?a=${encodeURI(newArtist)}&s=${encodeURI(newSong)}`;
        document.getElementById("image").setAttribute("src", imgPath);
        $("#image2").fadeOut(500,
            function() {
                document.getElementById("image2").setAttribute("src", imgPath);
                $("#image2").show();
            });

        if (newSong === "") {
            color = "130, 180, 255, 50";
        }
        var colors = color.split(",");

        if (document.getElementById("panelBottom") != null) {
            document.getElementById("panelBottom").style.background =
                `rgb(${colors[0]},${colors[1]},${colors[2]
                }, 50) linear-gradient(0deg, #00000056 0%, #000000b7 70%, #000000ff 100%)`;
        }

        if (document.getElementById("panelChat") != null) {
            document.getElementById("panelChat").style.backgroundColor =
                `rgba(${colors[0]},${colors[1]},${colors[2]}, 50)`;
        }

        if (document.getElementById("panelOverColor") != null) {
            document.getElementById("panelOverColor").style.backgroundColor =
                `rgba(${colors[0]},${colors[1]},${colors[2]}, 50)`;
        }

        if (document.getElementById("panelSideSmall") != null) {
            document.getElementById("panelSideSmall").style.backgroundColor =
                `rgba(${colors[0]},${colors[1]},${colors[2]}, 50)`;
        }

        if (document.getElementById("startingHeader") != null) {
            var hsl = RGBToHSL(colors[0], colors[1], colors[2]);
            document.getElementById("startingHeader").style.background =
                `linear-gradient(-20deg, hsl(${hsl[0]},${hsl[1]}%,${hsl[2]}%) -50%, #ffffff 80%)`;
            document.getElementById("startingHeader").style.webkitTextFillColor = "transparent";
            document.getElementById("startingHeader").style.webkitBackgroundClip = "text";
        }

        if (document.getElementById("panelBottomBackgroundAnimation") != null) {
            var hsl = RGBToHSL(colors[0], colors[1], colors[2]);
            document.getElementById("panelBottomBackgroundAnimation").style.filter =
                `hue-rotate(${hsl[0]}deg) saturate(${hsl[1]}%) brightness(${hsl[2]}%) opacity(0.4)`;
        }

        if (document.getElementById("chatBackgroundAnimation") != null) {
            var hsl = RGBToHSL(colors[0], colors[1], colors[2]);
            document.getElementById("chatBackgroundAnimation").style.filter =
                `hue-rotate(${hsl[0]}deg) saturate(${hsl[1]}%) brightness(${hsl[2]}%) opacity(0.4)`;
        }

        /*if (document.getElementById("song") != null) {
            var hsl = RGBToHSL(colors[0], colors[1], colors[2]);
            document.getElementById("song").style.background = "linear-gradient(-20deg, hsl(" + hsl[0] + "," + hsl[1] + "%," + (hsl[2]) + "%) -50%, #ffffff 80%)";
            document.getElementById("song").style.webkitTextFillColor = "transparent";
            document.getElementById("song").style.webkitBackgroundClip = "text";
            document.getElementById("songShadow").style.background = "linear-gradient(-20deg, hsv(" + hsl[0] + "," + hsl[1] + "%," + (hsl[2] / 2) + "%) -50%, #ffffff 80%)";
            document.getElementById("songShadow").style.webkitTextFillColor = "transparent";
            document.getElementById("songShadow").style.webkitBackgroundClip = "text";
        }*/

        if (document.getElementById("artist") != null) {
            var hsl = RGBToHSL(colors[0], colors[1], colors[2]);
            document.getElementById("artist").style.background =
                `linear-gradient(-20deg, hsl(${hsl[0]},${hsl[1]}%,${hsl[2]}%) -50%, #ffffff 100%)`;
            document.getElementById("artist").style.webkitTextFillColor = "transparent";
            document.getElementById("artist").style.webkitBackgroundClip = "text";
            document.getElementById("artistShadow").style.background =
                `linear-gradient(-20deg, hsv(${hsl[0]},${hsl[1]}%,${hsl[2] / 2}%) -50%, #ffffff 100%)`;
            document.getElementById("artistShadow").style.webkitTextFillColor = "transparent";
            document.getElementById("artistShadow").style.webkitBackgroundClip = "text";
        }

        if (document.getElementById("panelOverColorGradient") != null) {
            document.getElementById("panelOverColorGradient").style.background =
                `radial-gradient(1500px, rgba(${colors[0]},${colors[1]},${colors[2]}, 255), rgba(${colors[0]},${colors[
                    1]},${colors[2]}, 0))`;
            document.getElementById("panelOverColorGradient").style.backgroundRepeat = "no-repeat";
            document.getElementById("panelOverColorGradient").style.backgroundSize = "4000px 4000px";
            document.getElementById("panelOverColorGradient").style.backgroundPosition = "-1800px -2600px";
        }

        let headerTexts = document.getElementsByClassName("styledHeaderText");
        for (let i = 0; i < headerTexts.length; i++) {
            let h = headerTexts[i];
            var hsl = RGBToHSL(colors[0], colors[1], colors[2]);
            h.style.background = `linear-gradient(-20deg, hsl(${hsl[0]},${hsl[1]}%,${hsl[2]}%) -20%, #ffffff 100%)`;
            h.style.webkitTextFillColor = "transparent";
            h.style.webkitBackgroundClip = "text";
        }

        let styledTexts = document.getElementsByClassName("styledText");
        for (let i = 0; i < styledTexts.length; i++) {
            let h = styledTexts[i];
            var hsl = RGBToHSL(colors[0], colors[1], colors[2]);
            h.style.background = `linear-gradient(to top, rgb(104, 104, 104) 0%, #ffffff 60%)`;
            h.style.webkitTextFillColor = "transparent";
            h.style.webkitBackgroundClip = "text";
        }

        let songText = document.getElementById("song");
        if (songText != null) {
            songText.style.background = `linear-gradient(to top, rgb(104, 104, 104) 0%, #ffffff 60%)`;
            songText.style.webkitTextFillColor = "transparent";
            songText.style.webkitBackgroundClip = "text";
        }
    }
}

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


setInterval(() => {
        const heartRateElement = document.getElementById("heartRateText");
        const heartRateElementShadow = document.getElementById("heartRateTextShadow");

        if (heartRateElement == null || heartRateElementShadow == null)
            return;

        try {
            const response = fetch("/pulsoid/status", { method: "GET" });
            response.then((res) => {
                if (res.ok) {
                    res.json().then((status) => {
                        if (status == null || status === "") {
                            heartRateElement.innerText = "";
                            heartRateElementShadow.innerText = "";
                        } else {
                            heartRateElement.innerText = status;
                            heartRateElementShadow.innerText = status;
                        }
                    });
                } else {
                    console.error("An error occurred while fetching Spotify status");
                    heartRateElement.innerText = "";
                    heartRateElementShadow.innerText = "";
                }
            });
        } catch (error) {
            console.error("An error occurred while fetching Spotify status");
            heartRateElement.innerText = "";
            heartRateElementShadow.innerText = "";
        }
    },
    500);