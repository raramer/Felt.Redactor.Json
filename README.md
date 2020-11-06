# Redactor
Redactor is a set of libraries used to make it easy to mask sensitive information within structured text.

### How do I get started?

1. Configure the redactor.  In it's simplest form this is simply a list of property names whose values you want to mask.  
    ```csharp
    var redactor = new JsonRedactor("password");
    ```

2. Call the Redact method with the json structured text.          
    ```csharp
    var json = @"{
                   ""username"": ""jdoe"",
                   ""password": ""P@ssw0rd!""
                 }";
    var redactedJson = redactor.Redact(json); 
    ```
    ```
    {"username":"jdoe","password":"[REDACTED]"}
    ```

### Options

* Redacts : a list of fields whose values need to be masked.

See example above.

* IfIsRedacts : a list of rules for optional masking.

  * If : the name of a property you want to check.
  * Is : the expected value of the If property to enable masking of the Redact field.
  * Redact: the name of a field whose value needs to be masked. This can either be the If property or one of its siblings.

    ```csharp
    var redactor = new JsonRedactor(new IfIsRedact 
    { 
        If = "name", 
        Is = "password", 
        Redact = "value" 
    });

    var json = @"{
                   ""properties"": [
                     {
                       ""name"": ""username"",
                       ""value"": ""jdoe""
                     },
                     {
                       ""name"": ""password"",
                       ""value"": ""P@ssw0rd!""
                     },
                   ]
                 }";
    var redactedJson = redactor.Redact(json);
    ```
    ```
    {"properties":[{"name":"username","value":"jdoe"},{"name":"password","value":"[REDACTED]"}}
    ```

* Mask : the value used to mask redacted values. The default is "[REDACTED]", but you can provide the value you prefer.  e.g. "********"

* StringComparison : how to compare values. The default is OrdinalIgnoreCase.

* ComplexTypeHandling : how to mask complex types.
  * RedactValue (default) : redacts the entire value.
    ```
    {"total":19.99,"creditCard":"[REDACTED]"}
    ``` 
  * RedactDescendants : redacts each descendant value, preserving the complex type's structure.
    ```
    {"total":19.99,"creditCard":{"type":"[REDACTED]","number":"[REDACTED]","expiration":"[REDACTED]","cvv":"[REDACTED]" } }
    ``` 

* OnErrorRedact : how to handle the response when the value does not conform to the structured text type.
  * All (default) : redacts the entire response.
  * None : the original value is returned.

* Formatting : how to format the response
  * Compressed (default) : unnecessary whitespace is removed.
    ```
    {"username":"jdoe","password":"[REDACTED]"}
    ```
  * Indented : contains newlines and indented properties.
    ```
    {
        "username": "jdoe",
        "password": "[REDACTED]"
    }
    ```
