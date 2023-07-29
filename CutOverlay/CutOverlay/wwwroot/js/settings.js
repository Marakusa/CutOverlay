function saveConfig() {
    var config = {};

    var settingsFields = document.getElementsByClassName("settingsInput");
    for (var i = 0; i < settingsFields.length; i++) {
        var input = settingsFields[i].childNodes[3];
        config[input.id] = input.value;
    }
    console.log(config);

    try {
        var response = fetch("/configuration",
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

try {
    var response = fetch("/configuration", { method: "GET" });
    response.then((res) => {
        if (res.ok) {
            res.json().then((configurations) => {
                for (const fieldName in configurations) {
                    if (configurations.hasOwnProperty(fieldName)) {
                        var fieldValue = configurations[fieldName];
                        var input = document.getElementById(fieldName);
                        if (input) {
                            input.value = fieldValue;
                        }
                    }
                }
            });
        } else {
            console.error("Failed to save configuration.");
        }
    });
} catch (error) {
    console.error("An error occurred while saving the configuration.");
}

function spotifyDashboard() {
    fetch("/configuration/SpotifyDashboard", { method: "GET" });
}

function pulsoidDashboard() {
    fetch("/configuration/PulsoidDashboard", { method: "GET" });
}
