"use strict";

const DOMAIN = "https://your.signalr.service.net";

var scriptElement = document.currentScript;

var groupGuid =
  scriptElement.getAttribute("data-parameter") ??
  "9710a686-e94a-432f-87b8-60807fb8cc7e";

var userId =
  scriptElement.getAttribute("data-user") ??
  "88dab561-138b-489b-93d3-30a7c6e3d91f";

var userIdTo =
  scriptElement.getAttribute("data-touser") ??
  "46d1106f-a05a-4f19-8174-2ae30a1e7afe";


console.info({ group: groupGuid });

var connection = new signalR.HubConnectionBuilder()
  .withUrl("/chatHub/52328487-08a0-4b3e-ad73-99571262b25a", {
    headers: {
      "X-APP-ID": "34B57314-AEA4-4298-9935-199010D97DF1"
    }
  })
  .build();

//Disable the send button until connection is established.
document.getElementById("sendButton").disabled = true;
document.getElementById("sendPrivateButton").disabled = true;
document.getElementById("disconnectButton").disabled = true;

connection.on("ReceiveGroupMessage", function (fromUserGuid, toGroupGuid, message, messageGuid, date) {
  var li = document.createElement("li");
  document.getElementById("messagesList").appendChild(li);
  // We can assign user-supplied strings to an element's textContent because it
  // is not interpreted as markup. If you're assigning in any other way, you
  // should be aware of possible script injection concerns.
  li.textContent = `${user} says ${message}`;
});

connection.on("ReceivePrivateMessage", function (fromUserGuid, message, date) {
  alert(`Mensaje privado desde ${fromUserGuid} : ${date} - ${message}`)
});

connection.on("ReceiveDiagnostic", function (jsonObj, date) {
  console.info({json: jsonObj, date: date})
});

connection.on("DeleteMessage", function (messageGuid, sourceGuid, fromGroup) {
  console.info("DELETE", {messageGuid: messageGuid, sourceGuid: sourceGuid, fromGroup: fromGroup})
});

connection.on("UserConnected", function (userGuid) {
  console.info(`CONECTADO EL USUARIO ${userGuid}`)
});

connection
  .start()
  .then(function () {

    document.getElementById("sendButton").disabled = false;
    document.getElementById("sendPrivateButton").disabled = false;

    console.info({ Id: connection.connectionId });

    if (connection.connectionId === undefined) {
      return;
    }

    connection
      .invoke("AddUser", userId)
      .then(function (response){
        console.info(`User ${userId} joined. response`, response);

      connection
        .invoke("AddToGroup", groupGuid)
        .then(function (response) {

          document.getElementById("disconnectButton").disabled = false;
          
          console.info(`Connected to group ${groupGuid}`);
          
          connection
          .invoke("Online")
          .then(() => {
              console.info("Notificación online OK")
          });
        });

      })
      .catch(function (err) {
        return console.error("El error es", err.toString());
      });
  })
  .catch(function (err) {
    connection.stop();
    return console.error(err.toString());
  });

// ENVIO DE MENSAJE A GRUPO
document
  .getElementById("sendButton")
  .addEventListener("click", function (event) {
    var user = document.getElementById("userInput").value;
    var message = document.getElementById("messageInput").value;
    connection
      .invoke("SendGroupMessage", user, groupGuid, message)
      .catch(function (err) {
        return console.error(err.toString());
      });
    event.preventDefault();
  });

document
  .getElementById("diagnosticButton")
  .addEventListener("click", function (event) {

    console.info("Ejecutaándo diagnóstico");

    connection
      .invoke("Diagnostic")
      .then((response) => {
        console.info("Diagnóstico ejecutado", JSON.parse(response))
      })
      .catch(function (err) {
        return console.error(err.toString());
      });
    
    event.preventDefault();

  });
  
// ENVIO DE MENSAJES PRIVADOS
document
  .getElementById("sendPrivateButton")
  .addEventListener("click", function (event) {
    var message = document.getElementById("messageInput").value;
    connection
      .invoke("SendPrivateMessage", userId, userIdTo, message)
      .catch(function (err) {
        return console.error(err.toString());
      });
    event.preventDefault();
  });
  

document
  .getElementById("disconnectButton")
  .addEventListener("click", function (event) {
    connection.stop();
  });
