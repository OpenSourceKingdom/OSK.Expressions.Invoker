# OSK.Expressions.Invoker
Efficiently invoke custom compiled expressions for functions with varying parameter and return types. This library provides helper utilities to create the invokers for instance objects.

# Usage
The central focal point of the library is the `Invok6erFactory`. This static class provides a variety of APIs to generate an invoker that can efficiently, and quickly make method, property, and field calls on objects of any type.
For Property/Fields, it is assumed that the getter is intended to be used if the invoker is called without a parameter provided and the setter will be used if a parameter is provided.

# Contributions and Issues
Any and all contributions are appreciated! Please be sure to follow the branch naming convention OSK-{issue number}-{deliminated}-{branch}-{name} as current workflows rely on it for automatic issue closure. Please submit issues for discussion and tracking using the github issue tracker.