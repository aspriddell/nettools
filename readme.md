# NetTools
A bulk traceroute/ping output visualiser tool powered by `jc` and Blazor WebAssembly.

### Overview
This tool is a WebAssembly application that can accept one or more `jc`-processed traceroute/ping console outputs and
visualise them in a more human-readable format.

The website is fully static, and uses no server-side processing. IP address geolocation is the only exception to this,
where [ip-api.com](https://ip-api.com) is used when traceroute data is processed. It is then cached for performance and
to reduce likihood of ratelimiting.

#### Example Usage
> [!NOTE]
> You may be able to install `jc` via your package manager, see [supported package managers](https://github.com/kellyjonbrazil/jc?tab=readme-ov-file#os-package-repositories) for more info

```shell
# install jc if not already installed
pip3 install jc

# perform the ping, convert via jc, then save to a file
ping -c 100 google.com | jc --ping > ping-results.json

# open the website, select ping from the top bar then drag and drop the file into the dropzone
```

### Development State
The project is considered as mostly complete, and won't be actively developed except for bugfixes and community
contributions.
Feel free to open an issue or pull request if you have any suggestions or improvements.

### License
This project is licensed under the MIT License. See [license.md](license.md) for more information.