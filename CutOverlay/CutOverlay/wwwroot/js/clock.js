﻿setInterval(function () {
        const clockElement = document.getElementById('clockText');
        const currentTime = new Date();
        const currentTimeString = parseInt(currentTime.getHours()) + ':' + currentTime.getMinutes().toString().padStart(2, '0') + ':' + currentTime.getSeconds().toString().padStart(2, '0');
        clockElement.innerText = currentTimeString;
    },
    1000);