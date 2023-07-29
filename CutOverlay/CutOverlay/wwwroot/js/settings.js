function saveConfig() {
    const config = {};

    const settingsFields = document.getElementsByClassName("settingsInput");
    for (let i = 0; i < settingsFields.length; i++) {
        const input = settingsFields[i].childNodes[3];
        config[input.id] = input.value;
    }
    console.log(config);

    try {
        const response = fetch("/configuration",
            {
                method: "POST",
                headers: {
                    'Content-Type': "application/json"
                },
                body: JSON.stringify(config)
            });
        response.then((res) => {
            if (res.ok) {
                console.log("Configuration saved successfully.");
            } else {
                console.error("Failed to save configuration.");
            }
        });
    } catch (error) {
        console.error("An error occurred while saving the configuration.");
    }
}

function loadConfig() {
    try {
        const response = fetch("/configuration", { method: "GET" });
        response.then((res) => {
            if (res.ok) {
                res.json().then((configurations) => {
                    for (const fieldName in configurations) {
                        if (configurations.hasOwnProperty(fieldName)) {
                            const fieldValue = configurations[fieldName];
                            const input = document.getElementById(fieldName);
                            if (input) {
                                input.value = fieldValue;
                            }
                        }
                    }
                });
            } else {
                console.error("Failed to fetch configuration.");
            }
        });
    } catch (error) {
        console.error("An error occurred while saving the configuration.");
    }
}

function spotifyDashboard() {
    fetch("/configuration/SpotifyDashboard", { method: "GET" });
}

function pulsoidDashboard() {
    fetch("/configuration/PulsoidDashboard", { method: "GET" });
}

function copyToClipboard(id) {
    const inputElement = document.getElementById(id);
    inputElement.select();
    document.execCommand("copy");
}

loadConfig();
