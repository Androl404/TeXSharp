using System;
using System.Text;
using System.Collections.Generic;

public class WsMessageParser {
    // Represents the current state of message parsing
    private class MessageParsingState {
        public bool IsReceivingMessage { get; set; }
        public string ClientGuid { get; set; }
        public StringBuilder CurrentMessageContent { get; set; }
        public bool IsFullMessageComplete { get; set; }
    }

    // Tracks the current parsing state
    private MessageParsingState _currentState;

    public WsMessageParser() { ResetParsingState(); }

    // Reset the parsing state to initial conditions
    private void ResetParsingState() { _currentState = new MessageParsingState { IsReceivingMessage = false, ClientGuid = null, CurrentMessageContent = new StringBuilder(), IsFullMessageComplete = false }; }

    /// <summary>
    /// Parses an incoming message, handling potentially split full messages
    /// </summary>
    /// <param name="message">The incoming message string</param>
    /// <returns>A parsed message result</returns>
    public ParsedMessageResult ParseMessage(string message) {
        // Check for a relative message
        if (message.StartsWith("relative:START\n") && message.EndsWith("\nrelative:STOP\n")) {

            // Reset state and start a new message
            ResetParsingState();
            _currentState.IsReceivingMessage = true;
            _currentState.ClientGuid = null;
            _currentState.CurrentMessageContent.AppendLine(message.Replace("relative:START\n", "").Replace("\nrelative:STOP\n", ""));

            return new ParsedMessageResult { Type = MessageType.RelativeMessageComplete, ClientGuid = _currentState.ClientGuid, Content = _currentState.CurrentMessageContent.ToString() };
        }

        // Check for start of a full message
        if (message.StartsWith("full:") && message.Contains(":START\n")) {
            // Extract client GUID
            string clientGuid = ExtractClientGuid(message);

            // Reset state and start a new message
            ResetParsingState();
            _currentState.IsReceivingMessage = true;
            _currentState.ClientGuid = clientGuid;
            _currentState.CurrentMessageContent.Append(message.Replace("full:" + _currentState.ClientGuid + ":START\n", ""));

            if (message.Contains("\nfull:" + _currentState.ClientGuid + ":STOP\n")) {
                // Remove the stop marker from the message
                _currentState.CurrentMessageContent.Replace("\nfull:" + _currentState.ClientGuid + ":STOP\n", "");
                return new ParsedMessageResult { Type = MessageType.FullMessageComplete, ClientGuid = _currentState.ClientGuid, Content = _currentState.CurrentMessageContent.ToString() };
            } else {
                return new ParsedMessageResult { Type = MessageType.FullMessageStart, ClientGuid = clientGuid };
            }
        }

        // Check if we're currently receiving a message
        if (_currentState.IsReceivingMessage) {
            // Check for stop marker
            if (message.Contains("\nfull:" + _currentState.ClientGuid + ":STOP\n")) {
                // Remove the stop marker from the message
                string finalContent = message.Replace("\n\nfull:" + _currentState.ClientGuid + ":STOP\n", "");

                // Append final content if exists
                if (!string.IsNullOrEmpty(finalContent)) {
                    _currentState.CurrentMessageContent.AppendLine(finalContent);
                }

                // Mark message as complete
                _currentState.IsFullMessageComplete = true;

                // Return the complete message
                return new ParsedMessageResult { Type = MessageType.FullMessageComplete, ClientGuid = _currentState.ClientGuid, Content = _currentState.CurrentMessageContent.ToString() };
            } else {
                // Append the current message part
                _currentState.CurrentMessageContent.AppendLine(message);

                return new ParsedMessageResult { Type = MessageType.FullMessagePartial, ClientGuid = _currentState.ClientGuid };
            }
        }

        // If we reach here, the message is not relevant to our parsing
        return new ParsedMessageResult { Type = MessageType.Unrecognized };
    }

    /// <summary>
    /// Extracts the client GUID from a message start line
    /// </summary>
    private string? ExtractClientGuid(string message) {
        // Expected format: "full:Guid-client:START"
        string[] parts = message.Split(':');
        return parts.Length >= 2 ? parts[1] : null;
    }

    /// <summary>
    /// Checks if a full message is currently being processed
    /// </summary>
    public bool IsReceivingFullMessage => _currentState.IsReceivingMessage;

    /// <summary>
    /// Represents the result of parsing a message
    /// </summary>
    public class ParsedMessageResult {
        public MessageType Type { get; set; }
        public string ClientGuid { get; set; }
        public string Content { get; set; }
    }

    /// <summary>
    /// Represents the type of message parsed
    /// </summary>
    public enum MessageType {
        FullMessageStart,    // First part of a full message
        FullMessagePartial,  // Intermediate part of a full message
        FullMessageComplete, // Final part of a full message
        RelativeMessageComplete, // Full part of a relative message
        Unrecognized         // Message not related to full message parsing
    }
}
