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