# JSON to PHP converter

A JSON Object to PHP Array converter extension for [DevToys](https://devtoys.app/)

## Example

Configurable options (defaults in **bold**):
* Spacing: 2 spaces, **4 spaces**, tabs
* Quotes: **single** or double
* Trailing commas: **enable** or disable

Input:

```json
{
    "null": null,
    "false": false,
    "true": true,
    "string": "string",
    "integer": 100,
    "double": 1.0,
    "array": [1, 2, 3],
    "object": {
        "a": "one",
        "b": "two"
    }
}
```

Output:
```php
<?php

$json = [
    'null' => null,
    'false' => false,
    'true' => true,
    'string' => 'string',
    'integer' => 100,
    'double' => 1.0,
    'array' => [
        1,
        2,
        3,
    ],
    'object' => [
        'a' => 'one',
        'b' => 'two',
    ],
];
```

## License

This extension is licensed under the MIT License - see the [LICENSE](https://github.com/mickverm/DevToys.JsonPhpConverter/blob/main/LICENSE) file for details.

## Installation

1. Download the `Mickverm.DevToys.JsonPhpConverter` NuGet package from [NuGet.org](https://www.nuget.org/packages/Mickverm.DevToys.JsonPhpConverter/).
2. Open DevToys, go to `Manage extensions`, click on `Install an extension` and select the downloaded NuGet package.
