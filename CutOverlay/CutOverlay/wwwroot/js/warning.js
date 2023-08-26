var colorWarning;

const RGBToHSLWarning = (r, g, b) => {
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

function checkUpdateWarning() {
    try {
        const response = fetch("/status/get", { method: "GET" });
        response.then((res) => {
            if (res.ok) {
                res.json().then((status) => {

                    if (status == null || status.Status == null || status.Status.Paused) {
                        colorWarning = "130, 180, 255, 50";
                    } else {
                        colorWarning =
                            `${status.Song.Color.Red * 255}, ${status.Song.Color.Green * 255}, ${status.Song.Color.Blue *
                            255}, 255`;
                    }
                    displayWarningData();
                });
            } else {
                console.error("An error occurred while fetching song status");
                colorWarning = "130, 180, 255, 50";
                displayWarningData();
            }
        });
    } catch (error) {
        console.error("An error occurred while fetching song status");
        colorWarning = "130, 180, 255, 50";
        displayWarningData();
    }
}
setInterval(checkUpdateWarning, 2000);

function displayWarningData() {
    if (colorWarning == null) colorWarning = "130, 180, 255, 50";

    var colors = colorWarning.split(",");

    const startingHeaders = document.getElementsByClassName("startingHeader");
    for (var j = 0; j < startingHeaders.length; j++) {
        var hsl = RGBToHSLWarning(colors[0], colors[1], colors[2]);
        startingHeaders[j].style.background =
            `linear-gradient(-20deg, hsl(${hsl[0]},${hsl[1]}%,${hsl[2]}%) -50%, #ffffff 80%)`;
        startingHeaders[j].style.webkitTextFillColor = "transparent";
        startingHeaders[j].style.webkitBackgroundClip = "text";
    }
}

function startingHeaderWarning() {
    let paragraphs = $('.startingHeader');
    let currentIndex = 0;

    function fadeNext() {
        paragraphs.removeClass('active');
        paragraphs.eq(currentIndex).addClass('active');
        currentIndex = (currentIndex + 1) % paragraphs.length;
    }

    fadeNext(); // Show the first paragraph initially

    setInterval(fadeNext, 10000); // Change paragraph every 5 seconds (adjust as needed)
}

startingHeaderWarning();
