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

const HexToRGB = (hex) => {
    const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
    return result
        ? {
            r: parseInt(result[1], 16),
            g: parseInt(result[2], 16),
            b: parseInt(result[3], 16)
        }
        : null;
}

var themeColor;

function checkColorUpdate() {
    try {
        const response = fetch("/status/get", { method: "GET" });
        response.then((res) => {
            if (res.ok) {
                res.json().then((status) => {

                    if (status == null || status.Status == null || status.Status.Paused) {
                        themeColor = "130, 180, 255, 50";
                    } else {
                        themeColor =
                            `${status.Song.Color.Red * 255}, ${status.Song.Color.Green * 255}, ${status.Song.Color.Blue *
                            255}, 255`;
                    }
                    themeColorUpdate();
                });
            } else {
                console.error("An error occurred while fetching song status");
                themeColor = "130, 180, 255, 50";
                themeColorUpdate();
            }
        });
    } catch (error) {
        console.error("An error occurred while fetching song status");
        themeColor = "130, 180, 255, 50";
        themeColorUpdate();
    }
}

setInterval(checkColorUpdate, 2000);

function themeColorUpdate() {
    if (themeColor == null) themeColor = "130, 180, 255, 50";

    const colors = themeColor.split(",");
    const hslColors = RGBToHSL(colors[0], colors[1], colors[2]);

    const headerTexts = document.getElementsByClassName("styledHeaderText");
    for (let i = 0; i < headerTexts.length; i++) {
        const h = headerTexts[i];
        h.style.background =
            `linear-gradient(-20deg, hsl(${hslColors[0]},${hslColors[1]}%,${hslColors[2]}%) -20%, #ffffff 100%)`;
        h.style.webkitTextFillColor = "transparent";
        h.style.webkitBackgroundClip = "text";
    }

    const styledTexts = document.getElementsByClassName("styledText");
    for (let i = 0; i < styledTexts.length; i++) {
        const h = styledTexts[i];
        h.style.background = `white`;
        h.style.webkitTextFillColor = "transparent";
        h.style.webkitBackgroundClip = "text";
    }
}
