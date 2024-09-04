# NetTools
A bulk traceroute/ping output visualiser tool.

### Overview
This tool is a WebAssembly application that can accept one or more `jc`-processed traceroute/ping console outputs and
visualise them in a more human-readable format.

The website is fully static, and uses no server-side processing. IP address geolocation is the only exception to this,
where [ip-api.com](https://ip-api.com) is used when traceroute data is processed. It is then cached for performance and
to reduce likihood of ratelimiting.

### Development State
The project is considered as mostly complete, and won't be actively developed except for bugfixes and community
contributions.
Feel free to open an issue or pull request if you have any suggestions or improvements.

### License
This project is licensed under the MIT License. See [license.md](license.md) for more information.