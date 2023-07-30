const draggableWindows = document.getElementsByClassName("draggableWindow");

function dragElement(element) {
    const header = document.getElementById(element.id + "Header");

    var pos1 = 0, pos2 = 0, pos3 = 0, pos4 = 0;
    header.onmousedown = dragMouseDown;

    function dragMouseDown(e) {
        if (e.srcElement.className === "closeWindowButton")
            return;
        e = e || window.event;
        e.preventDefault();
        // get the mouse cursor position at startup:
        pos3 = e.clientX;
        pos4 = e.clientY;
        document.onmouseup = closeDragElement;
        // call a function whenever the cursor moves:
        document.onmousemove = elementDrag;
    }

    function elementDrag(e) {
        e = e || window.event;
        e.preventDefault();
        // calculate the new cursor position:
        pos1 = pos3 - e.clientX;
        pos2 = pos4 - e.clientY;
        pos3 = e.clientX;
        pos4 = e.clientY;
        // set the element's new position:
        let top = element.offsetTop - pos2;
        let left = element.offsetLeft - pos1;
        if (top < 0) top = 0;
        if (left < 0) left = 0;
        if (top > window.innerHeight - 30) top = window.innerHeight - 30;
        if (left > window.innerWidth - 30) left = window.innerWidth - 30;
        element.style.top = top + "px";
        element.style.left = left + "px";
    }

    function closeDragElement() {
        // stop moving when mouse button is released:
        document.onmouseup = null;
        document.onmousemove = null;
    }
}

var windowsList = [];

function windowMouseDown(e) {
    let parent = e.srcElement;
    let found = false;
    while (!found && parent.parentElement != null) {
        if (parent.className === "draggableWindow") {
            found = true;
            continue;
        }
        parent = parent.parentElement;
    }

    setWindowTop(parent);
    
    windowsList.splice(windowsList.indexOf(parent), 1);
    windowsList.push(parent);

    for (let i = 0; i < windowsList.length; i++) {
        windowsList[i].style.zIndex = i + 11;
    }
}

for (let i = 0; i < draggableWindows.length; i++) {
    draggableWindows[i].style.zIndex = i + 11;
    dragElement(draggableWindows[i]);
    windowsList.push(draggableWindows[i]);
    draggableWindows[i].onmousedown = windowMouseDown;
}

function setWindowTop(window) {
    windowsList.splice(windowsList.indexOf(window), 1);
    windowsList.push(window);

    for (let i = 0; i < windowsList.length; i++) {
        windowsList[i].style.zIndex = i + 11;
    }
}

function showWindow(id, show) {
    document.getElementById(id).style.display = show ? null : "none";
    setWindowTop(document.getElementById(id));
}
