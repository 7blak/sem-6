package org.exceptions;

public class InvalidMessageContentException extends RuntimeException {

  public InvalidMessageContentException(final Throwable cause) {
    super("Could't parse message body due to the invalid content.", cause);
  }
}
