using System;

public class TEA_Exception : System.Exception {
 public TEA_Exception(string message) : base(message) {
 }

 public TEA_Exception(string message, Exception innerException) : base(message, innerException) {
 }
}
