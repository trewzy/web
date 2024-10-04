function message() {
    let name = document.getElementById("user").value;
    let message = document.getElementById("content").value;

    const webhook = "https://discord.com/api/webhooks/1291826937714446359/qgBUIqEdpr1tiJctINfucv78F9bQW2luRalyxw0tB0EdeuJONTTwj3JlcB0QXLVMIL_l";
    const contents = `Name: ${name}\nMessage: ${message}`;
    const request = new XMLHttpRequest();
    request.open("POST", webhook);
    request.setRequestHeader('Content-type', 'application/json');

    const params = {
        "content": null,
        "embeds": [
            {
            "title": "Someone has contacted you",
            "description": contents,
            "color": 16250871
            }
            ],
            "attachments": []
    }
    request.send(JSON.stringify(params));

    console.warn("niggerr")
}