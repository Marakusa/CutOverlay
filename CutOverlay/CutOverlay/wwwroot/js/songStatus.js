var newSong = "";
var newArtist = "";
var followersSince = "";
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
    document.getElementById("song").innerHTML = newSong;
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

function checkUpdate() {
    // Hide song progress text if no song playing
    const progressText = document.getElementById("progressText");
    if (newSong === "" && progressText != null) {
        progressText.parentElement.parentElement.style.display = "none";
    }

    try {
        const response = fetch("/status/get", { method: "GET" });
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
                console.error("An error occurred while fetching song status");
                emptyConfig();
                displayData();
            }
        });
    } catch (error) {
        console.error("An error occurred while fetching song status");
        emptyConfig();
        displayData();
    }
}

setInterval(checkUpdate, 2000);

function emptyConfig() {
    newArtist = "";
    newSong = "";
    color = `0, 0, 0, 255`;
    progressData = null;

    const progressText = document.getElementById("progressText");
    if (progressText != null) {
        progressText.parentElement.parentElement.style.display = "none";
    }
}

// Handle the music progress bar
setInterval(function() {
        try {
            if (progressData == null || newSong === "" || progressData.Progress === 0 || progressData.Total === 0)
                return;

            // Get the current timestamp
            const currentTime = Math.floor(Date.now() / 1000);

            // Calculate the elapsed time since FetchTime
            const elapsedSeconds = (progressData.Progress / 1000) + currentTime - progressData.FetchTime;
            const elapsedMilliseconds = progressData.Progress + Date.now() - (progressData.FetchTime * 1000);

            // Calculate the progress based on elapsed time
            let progressPercentage = ((elapsedSeconds * 1000.0) / progressData.Total);
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
            const progressString =
                progressMinutes + ":" + (progressSeconds < 10 ? `0${progressSeconds}` : progressSeconds);

            const progressText = progressString + " / " + totalString;

            const progressBar = document.getElementById("progress");
            if (progressBar != null) {
                // Update the progress bar width
                progressBar.style.width = ((elapsedMilliseconds / progressData.Total) * 100) + "%";
            }

            // Update the progress text
            const progressTextElement = document.getElementById("progressText");
            if (progressTextElement != null) {
                progressTextElement.innerHTML = progressText;

                // Update the progress header text
                document.getElementById("progressHeader").innerHTML = "Track progress";

                // Show progress text element
                progressTextElement.parentElement.parentElement.style.display = null;
            }
        } catch (ex) {
            console.error(ex);
        }
    },
    500);

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

        var imgPath = `/status/image?a=${encodeURI(newArtist)}&s=${encodeURI(newSong)}`;
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
        var hslColors = RGBToHSL(colors[0], colors[1], colors[2]);
        
        if (document.getElementById("artist") != null) {
            document.getElementById("artist").style.background =
                `linear-gradient(-20deg, hsl(${hslColors[0]},${hslColors[1]}%,${hslColors[2]}%) -50%, #ffffff 100%)`;
            document.getElementById("artist").style.webkitTextFillColor = "transparent";
            document.getElementById("artist").style.webkitBackgroundClip = "text";
        }

        let songText = document.getElementById("song");
        if (songText != null) {
            songText.style.background = `white`;
            songText.style.webkitTextFillColor = "transparent";
            songText.style.webkitBackgroundClip = "text";
        }
    }
}