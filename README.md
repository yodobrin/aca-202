# Implementing a 202 pattern with Azure Container Apps

The 202 pattern is a design pattern that is used to handle requests that may take a long time to complete. It involves returning a 202 Accepted response to the client, indicating that the request has been accepted and is being processed asynchronously. The client can then poll the server for the status of the request using a separate API.

This pattern can be used in scenarios where the request processing time may vary significantly, or where the client does not need to wait for the request to complete before performing other tasks. It can also be used to decouple the client from the server, allowing the client to continue processing without being blocked by the server.

The 202 pattern is commonly used in distributed systems, where the client and server may be running on different machines or in different locations. It can also be useful in microservices architectures, where different services may need to communicate with each other to complete a request.


__What would be covered?__