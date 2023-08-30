var colorWarning;

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
                            `${status.Song.Color.Red * 255}, ${status.Song.Color.Green * 255}, ${
                            status.Song.Color.Blue *
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

    const colors = colorWarning.split(",");

    const startingHeaders = document.getElementsByClassName("startingHeader");
    for (let j = 0; j < startingHeaders.length; j++) {
        const hsl = RGBToHSL(colors[0], colors[1], colors[2]);
        startingHeaders[j].style.background =
            `linear-gradient(-20deg, hsl(${hsl[0]},${hsl[1]}%,${hsl[2]}%) -50%, #ffffff 80%)`;
        startingHeaders[j].style.webkitTextFillColor = "transparent";
        startingHeaders[j].style.webkitBackgroundClip = "text";
    }
}

function startingHeaderWarning() {
    const paragraphs = $(".startingHeader");
    let currentIndex = 0;

    function fadeNext() {
        paragraphs.removeClass("active");
        paragraphs.eq(currentIndex).addClass("active");
        currentIndex = (currentIndex + 1) % paragraphs.length;
    }

    fadeNext(); // Show the first paragraph initially

    setInterval(fadeNext, 10000); // Change paragraph every 5 seconds (adjust as needed)
}

startingHeaderWarning();