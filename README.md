![Icon](https://raw.github.com/SzymonPobiega/NServiceBus.Raw/master/icons/dune-buggy.png)

# NServiceBus.Raw

Sending and receiving raw messages using NServiceBus transport infrastructure. NServiceBus to messaging is like Nissan Patrol to off-roading -- a full-featured and mature tool that has all the things you might ever need. NServiceBus.Raw, on the other hand, is like an off-road buggy. It has the same Nissan Patrol super-durable axles and engine but offers no ammenities other than a chair and a steering wheel.

## Configuration

## Sending

The following code sends a message to another endpoint

```
var headers = new Dictionary<string, string>();
var body = GetMessageBody();
headers["SomeHeader"] = "SomeValue";
var request = new OutgoingMessage(Guid.NewGuid().ToString(), headers, body);

var operation = new TransportOperation(
    request, 
    new UnicastAddressTag("Receiver"));

await sender.SendRaw(
    new TransportOperations(operation), //Can have multiple sends in one batch
    new TransportTransaction(), 
    new ContextBag())
    .ConfigureAwait(false);
```

## Receiving

## Icon

[Dune Buggy](https://thenounproject.com/term/dune-buggy/40630/) by [Iain Hector](https://thenounproject.com/iainhector/) from the Noun Project
