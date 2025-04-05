using System;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// Parses WebSocket messages, supporting both full and relative message formats
/// from clients identified by GUIDs. Handles message reconstruction across multiple frames.
/// </summary>
public class WsMessageParser {
    /// <summary>
    /// Represents the current internal state during the parsing of a message.
    /// </summary>
    private class MessageParsingState {
        /// <value>Boolean: if a message is currently being send by a client.</value>
        public bool IsReceivingMessage { get; set; }
        /// <value>A string containing the client <c>Guid</c>.</value>
        public string ClientGuid { get; set; }
        /// <value>A string builder to build the message we are currently receiving.</value>
        public StringBuilder CurrentMessageContent { get; set; }
        /// <value>Boolean: if a message is completely send and received .</value>
        public bool IsFullMessageComplete { get; set; }
    }

    /// <value>Tracks the ongoing message parsing state.</value>
    private MessageParsingState _currentState;

    /// <summary>
    /// Initializes a new instance of the <see cref="WsMessageParser"/> class. This is the constructor of the class.
    /// </summary>
    public WsMessageParser() { ResetParsingState(); }

    /// <summary>
    /// Resets the internal state to prepare for a new message.
    /// </summary>
    /// <returns>This methods does not return anything.</returns>
    private void ResetParsingState() { _currentState = new MessageParsingState { IsReceivingMessage = false, ClientGuid = null, CurrentMessageContent = new StringBuilder(), IsFullMessageComplete = false }; }

    /// <summary>
    /// Parses an incoming WebSocket message and returns a structured result.
    /// Handles full messages sent across multiple parts, and single-part relative messages.
    /// </summary>
    /// <param name="message">The raw message received over WebSocket.</param>
    /// <returns>A <see cref="ParsedMessageResult"/> representing the interpreted content.</returns>
    public ParsedMessageResult ParseMessage(string message) {
        // Handle relative message format: one-shot message block
        if (message.StartsWith("relative:START\n") && message.EndsWith("\nrelative:STOP\n")) {
            ResetParsingState();
            _currentState.IsReceivingMessage = true;
            _currentState.ClientGuid = null;

            _currentState.CurrentMessageContent.AppendLine(message.Replace("relative:START\n", "").Replace("\nrelative:STOP\n", ""));

            return new ParsedMessageResult { Type = MessageType.RelativeMessageComplete, ClientGuid = _currentState.ClientGuid, Content = _currentState.CurrentMessageContent.ToString() };
        }

        // Handle start of a full multi-part message
        if (message.StartsWith("full:") && message.Contains(":START\n")) {
            string clientGuid = ExtractClientGuid(message);

            ResetParsingState();
            _currentState.IsReceivingMessage = true;
            _currentState.ClientGuid = clientGuid;
            _currentState.CurrentMessageContent.Append(message.Replace($"full:{clientGuid}:START\n", ""));

            if (message.Contains($"\nfull:{clientGuid}:STOP\n")) {
                _currentState.CurrentMessageContent.Replace($"\nfull:{clientGuid}:STOP\n", "", 0, _currentState.CurrentMessageContent.Length);
                return new ParsedMessageResult { Type = MessageType.FullMessageComplete, ClientGuid = clientGuid, Content = _currentState.CurrentMessageContent.ToString() };
            }

            return new ParsedMessageResult { Type = MessageType.FullMessageStart, ClientGuid = clientGuid };
        }

        // Handle continuation of a full message
        if (_currentState.IsReceivingMessage) {
            if (message.Contains($"\nfull:{_currentState.ClientGuid}:STOP\n")) {
                string finalContent = message.Replace($"\n\nfull:{_currentState.ClientGuid}:STOP\n", "");

                if (!string.IsNullOrEmpty(finalContent))
                    _currentState.CurrentMessageContent.AppendLine(finalContent);

                _currentState.IsFullMessageComplete = true;

                return new ParsedMessageResult { Type = MessageType.FullMessageComplete, ClientGuid = _currentState.ClientGuid, Content = _currentState.CurrentMessageContent.ToString() };
            } else {
                _currentState.CurrentMessageContent.AppendLine(message);
                return new ParsedMessageResult { Type = MessageType.FullMessagePartial, ClientGuid = _currentState.ClientGuid };
            }
        }

        // Fallback case for unexpected message format
        return new ParsedMessageResult { Type = MessageType.Unrecognized };
    }

    /// <summary>
    /// Extracts the GUID from a full message's header.
    /// </summary>
    /// <param name="message">The full message string.</param>
    /// <returns>The client GUID as a string, or null if not present.</returns>
    private string? ExtractClientGuid(string message) {
        string[] parts = message.Split(':');
        return parts.Length >= 2 ? parts[1] : null;
    }

    /// <value>Indicates whether the parser is currently processing a full message.</value>
    public bool IsReceivingFullMessage => _currentState.IsReceivingMessage;

    /// <summary>
    /// Represents the result of parsing a WebSocket message.
    /// </summary>
    public class ParsedMessageResult {
        /// <value>The type of message identified.</value>
        public MessageType Type { get; set; }

        /// <value>The GUID of the client that sent the message (may be null for relative messages).</value>
        public string ClientGuid { get; set; }

        /// <value>The reconstructed message content.</value>
        public string Content { get; set; }
    }

    /// <summary>
    /// Enum indicating the kind of message being parsed.
    /// </summary>
    public enum MessageType {
        /// <value>First part of a multi-part message.</value>
        FullMessageStart,

        /// <value>Intermediate part of a multi-part message.</value>
        FullMessagePartial,

        /// <value>Final part of a multi-part message.</value>
        FullMessageComplete,

        /// <value>Complete single relative message.</value>
        RelativeMessageComplete,

        /// <value>Message was not recognized or does not follow a known format.</value>
        Unrecognized
    }
}
