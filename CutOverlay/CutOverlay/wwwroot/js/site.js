var newSong;
var newArtist;
var color;
var shown = false;
function hideText() {
    $("#artistNameDiv").animate({
        marginLeft: "-100px",
        opacity: 0
    }, 300);
    $("#songNameDiv").animate({
        marginLeft: "-100px",
        opacity: 0
    }, 300);

    document.getElementById("songNameDiv").classList.remove("scrolling");
    document.getElementById("artistNameDiv").classList.remove("scrolling");
}

function updateText() {
    document.getElementById("artist").innerHTML = newArtist;
    document.getElementById("artistShadow").innerHTML = newArtist;
    document.getElementById("song").innerHTML = newSong.substring(1, newSong.length - 1);
    document.getElementById("songShadow").innerHTML = newSong.substring(1, newSong.length - 1);
}

var progressData;

function showText() {
    $("#artistNameDiv").animate({
        marginLeft: "-4px",
        opacity: 1
    }, 300);
    $("#songNameDiv").animate({
        marginLeft: "85px",
        opacity: 1
    }, 300);
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

    $.get("../Snip_Artist.txt", function (art) {
        newArtist = art.replace(/&/g, "&amp;");
    }).then(
        $.get("../Snip_Track.txt", function (sng) {
            newSong = sng.replace(/&/g, "&amp;");
        })).then(
            $.get("../Snip_Color.txt", function (sng) {
                color = sng.replace(/&/g, "&amp;");
                if (color == "")
                    color = "255, 255, 255, 0";
            })).then(displayData);

    $.get("../Snip_Progress.txt", function (data) {
        try {
            // Example JSON data
            progressData = JSON.parse(data);
        }
        catch (ex) {
            progressData = null;
            return;
        }
    });

    setTimeout(checkUpdate, 2000);
}

// Handle the music progress bar
setInterval(function () {
    if (progressData == null)
        return;

    const jsonData = progressData;

    // Get the current timestamp
    const currentTime = Math.floor(Date.now() / 1000);

    // Calculate the elapsed time since FetchTime
    const elapsedSeconds = (jsonData.Progress / 1000) + currentTime - jsonData.FetchTime;
    const elapsedMilliseconds = jsonData.Progress + Date.now() - (jsonData.FetchTime * 1000);

    // Calculate the progress based on elapsed time
    var progressPercentage = ((elapsedSeconds * 1000) / jsonData.Total);
    if (progressPercentage > 1)
        progressPercentage = 1;
    else if (progressPercentage < 0)
        progressPercentage = 0;

    // Convert progress percentage to 0:00 format
    const adjustedProgress = Math.floor(progressPercentage * (jsonData.Total / 1000));

    // Convert total to 0:00 format
    const totalMinutes = Math.floor(jsonData.Total / 60000);
    const totalSeconds = Math.floor((jsonData.Total - totalMinutes * 60000) / 1000);
    const totalString = totalMinutes + ":" + (totalSeconds < 10 ? "0" + totalSeconds : totalSeconds);

    // Convert progress to 0:00 format
    const progressMinutes = Math.floor((adjustedProgress) / 60);
    const progressSeconds = Math.floor(((adjustedProgress) - progressMinutes * 60));
    const progressString = progressMinutes + ":" + (progressSeconds < 10 ? "0" + progressSeconds : progressSeconds);

    const progressText = progressString + " / " + totalString;

    // Update the progress bar width
    const progressBar = document.getElementById("progress");
    progressBar.style.width = ((elapsedMilliseconds / jsonData.Total) * 100) + "%";

    // Update the progress text
    const progressTextElement = document.getElementById("progressText");
    progressTextElement.innerHTML = progressText;
    const progressTextSghadowElement = document.getElementById("progressTextShadow");
    progressTextSghadowElement.innerHTML = progressText;

    // Update the progress header text
    document.getElementById("progressHeader").innerHTML = "Track progress";
    document.getElementById("progressHeaderShadow").innerHTML = "Track progress";
}, 100);

function displayData() {
    if ((newSong == "" && newSong != document.getElementById("song").innerHTML) || newSong.substring(1, newSong.length - 1) != document.getElementById("song").innerHTML) {
        if (newSong.length > 1 && !shown) {
            $("#bigdiv").animate({
                marginLeft: "0px",
            }, 500)
            shown = true;
        }
        if (newSong.length < 1 && shown) {
            $("#bigdiv").animate({
                marginLeft: "-500px",
            }, 500)
            shown = false;
        }
        console.log("New song, old song: " + document.getElementById("song").innerHTML + " new song: " + newSong);
        hideText();
        setTimeout(updateText, 300);
        setTimeout(showText, 400);
        var imgpath = "../Snip_Artwork.jpg?t=" + newSong + newArtist;
        document.getElementById("image").setAttribute("src", imgpath);
        $("#image2").fadeOut(500, function () {
            document.getElementById("image2").setAttribute("src", imgpath);
            $("#image2").show();
        });

        if (newSong == "") {
            color = "130, 180, 255, 50";
        }
        var colors = color.split(",");

        if (document.getElementById("panelBottom") != null) {
            document.getElementById("panelBottom").style.backgroundColor = "rgba(" + colors[0] + "," + colors[1] + "," + colors[2] + ", 50)";
        }

        if (document.getElementById("panelChat") != null) {
            document.getElementById("panelChat").style.backgroundColor = "rgba(" + colors[0] + "," + colors[1] + "," + colors[2] + ", 50)";
        }

        if (document.getElementById("panelOverColor") != null) {
            document.getElementById("panelOverColor").style.backgroundColor = "rgba(" + colors[0] + "," + colors[1] + "," + colors[2] + ", 50)";
        }

        if (document.getElementById("panelSideSmall") != null) {
            document.getElementById("panelSideSmall").style.backgroundColor = "rgba(" + colors[0] + "," + colors[1] + "," + colors[2] + ", 50)";
        }

        if (document.getElementById("startingHeader") != null) {
            var hsl = RGBToHSL(colors[0], colors[1], colors[2]);
            document.getElementById("startingHeader").style.background = "linear-gradient(-20deg, hsl(" + hsl[0] + "," + hsl[1] + "%," + (hsl[2]) + "%) -50%, #ffffff 80%)";
            document.getElementById("startingHeader").style.webkitTextFillColor = "transparent";
            document.getElementById("startingHeader").style.webkitBackgroundClip = "text";
        }

        if (document.getElementById("panelBottomBackgroundAnimation") != null) {
            var hsl = RGBToHSL(colors[0], colors[1], colors[2]);
            document.getElementById("panelBottomBackgroundAnimation").style.filter = "hue-rotate(" + hsl[0] + "deg) saturate(" + hsl[1] + "%) brightness(" + hsl[2] + "%) opacity(0.4)";
        }

        if (document.getElementById("chatBackgroundAnimation") != null) {
            var hsl = RGBToHSL(colors[0], colors[1], colors[2]);
            document.getElementById("chatBackgroundAnimation").style.filter = "hue-rotate(" + hsl[0] + "deg) saturate(" + hsl[1] + "%) brightness(" + hsl[2] + "%) opacity(0.4)";
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
            document.getElementById("artist").style.background = "linear-gradient(-20deg, hsl(" + hsl[0] + "," + hsl[1] + "%," + (hsl[2]) + "%) -50%, #ffffff 100%)";
            document.getElementById("artist").style.webkitTextFillColor = "transparent";
            document.getElementById("artist").style.webkitBackgroundClip = "text";
            document.getElementById("artistShadow").style.background = "linear-gradient(-20deg, hsv(" + hsl[0] + "," + hsl[1] + "%," + (hsl[2] / 2) + "%) -50%, #ffffff 100%)";
            document.getElementById("artistShadow").style.webkitTextFillColor = "transparent";
            document.getElementById("artistShadow").style.webkitBackgroundClip = "text";
        }

        if (document.getElementById("panelOverColorGradient") != null) {
            document.getElementById("panelOverColorGradient").style.background = "radial-gradient(1500px, rgba(" + colors[0] + "," + colors[1] + "," + colors[2] + ", 255), rgba(" + colors[0] + "," + colors[1] + "," + colors[2] + ", 0))";
            document.getElementById("panelOverColorGradient").style.backgroundRepeat = "no-repeat";
            document.getElementById("panelOverColorGradient").style.backgroundSize = "4000px 4000px";
            document.getElementById("panelOverColorGradient").style.backgroundPosition = "-1800px -2600px";
        }

        if (document.getElementById("progressHeader") != null) {
            var hsl = RGBToHSL(colors[0], colors[1], colors[2]);
            document.getElementById("progressHeader").style.background = "linear-gradient(-20deg, hsl(" + hsl[0] + "," + hsl[1] + "%," + (hsl[2]) + "%) -10%, #ffffff 20%)";
            document.getElementById("progressHeader").style.webkitTextFillColor = "transparent";
            document.getElementById("progressHeader").style.webkitBackgroundClip = "text";
        }

        if (document.getElementById("timezoneText") != null) {
            var hsl = RGBToHSL(colors[0], colors[1], colors[2]);
            document.getElementById("timezoneText").style.background = "linear-gradient(-20deg, hsl(" + hsl[0] + "," + hsl[1] + "%," + (hsl[2]) + "%) -10%, #ffffff 20%)";
            document.getElementById("timezoneText").style.webkitTextFillColor = "transparent";
            document.getElementById("timezoneText").style.webkitBackgroundClip = "text";
        }

        if (document.getElementById("heartRate") != null) {
            var hsl = RGBToHSL(colors[0], colors[1], colors[2]);
            document.getElementById("heartRate").style.background = "linear-gradient(-20deg, hsl(" + hsl[0] + "," + hsl[1] + "%," + (hsl[2]) + "%) -10%, #ffffff 20%)";
            document.getElementById("heartRate").style.webkitTextFillColor = "transparent";
            document.getElementById("heartRate").style.webkitBackgroundClip = "text";
        }
    }
}

function hexToRgb(hex) {
    const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
    return result ? {
        r: parseInt(result[1], 16),
        g: parseInt(result[2], 16),
        b: parseInt(result[3], 16)
    } : null;
}
