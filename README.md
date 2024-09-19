# EasyCaption - Quick image captioning in Windows

![image](https://github.com/user-attachments/assets/fe5e83dc-f4b3-4f3a-8a1f-c86dd42ccae7)


This is a simple tool to allow you to right-click an image in Windows File Explorer and generated/save captions for that image to a text file alongside it. It uses APIs (run separately) for Florence 2 captioning and optionally an additional LLM API to clean up the caption to remove annoying things like "This is an image of" or "in the style of a digital illustration". You can configure whether it makes the second call to clean up the caption - it's quicker if you don't run this, but the caption is much cleaner if you do.

# Installation

Download and install .Net 8.0:

https://dotnet.microsoft.com/en-us/download/dotnet/8.0

Download the latest release file [here](https://github.com/morphicschris/easycaption/releases). Extract it to a folder on your computer. The default is "C:\EasyCaption\" - you can put it wherever you want but if you change this directory you need to make sure you update the registry file below accordingly. Your folder structure should look something like this:

```
C:\EasyCaption\
- Add_to_explorer_context_VIEW_README_FIRST.reg
- appsettings.json
- EasyCaption.deps.json
- EasyCaption.dll
- EasyCaption.exe
- EasyCaption.pdb
- EasyCaption.runtimeconfig.json
- Microsoft.Extensions.Configuration.Abstractions.dll
- Microsoft.Extensions.Configuration.Binder.dll
- Microsoft.Extensions.Configuration.dll
- Microsoft.Extensions.Configuration.FileExtensions.dll
- Microsoft.Extensions.Configuration.Json.dll
- Microsoft.Extensions.FileProviders.Abstractions.dll
- Microsoft.Extensions.FileProviders.Physical.dll
- Microsoft.Extensions.FileSystemGlobbing.dll
- Microsoft.Extensions.Primitives.dll
```

If you extracted it in the default directory you're almost good to go. Just double-click the "Add_to_explorer_context_VIEW_README_FIRST.reg" file to add the "Capture" context menu item on .png and .jpg files. If you changed the directory where you extracted the files, open up the .reg file and modify lines 9 and 17 to point to your directory:

`@="\"C:\\EasyCaption\\EasyCaption.exe\" \"%1\""`

# Configuration

The default API endpoint for Florence 2 is:

`http://localhost:5001/api/caption`

The default API endpoint for the LLM re-captioning uses OpenAI API format and is:

`http://localhost:5000/v1/chat/completions`

You can change both of these in the appsettings.json file.

You will need to be running the APIs for Florence 2 and the LLM. This can be done either locally on your PC, using a cloud provider, or from another networked PC. You can specify new endpoints in the appsettings.json configuration file.

If running locally, both of these can be run on CPU if you don't want to take up your GPU - I use the flocap Docker image to run Florence 2 locally, and LM Studio to run an LLM API. Make sure you specify the correct ports when running them, as they both default to 5000 and will conflict. This is why I run flocap on 5001.

https://github.com/wegwerfen/flocap

https://lmstudio.ai/

I'm using the Llama 3.1 8B Instruct model in LM Studio. You can use your model of choice but this one is fairly lightweight and handles the re-captioning well.

# Running the captioning

With all that in place and your APIs running, all you need to do now is right-click an image file and select the "Caption" option. After a few seconds you should have a .txt file alongside your image with nice natural language captions in!

# Example output

![ComfyUI_temp_mkzml_00032_](https://github.com/user-attachments/assets/fb977b6b-bd1e-4183-8a1c-def9e09ba230)

```
A group of red poppies on a black background. The flowers are in full bloom, with their petals open wide and their stems and leaves visible. The background is a gradient of black and white, with some areas appearing to be splattered with red paint. The overall mood of the scene is dark and moody, with the red flowers standing out against the dark background.
```


# Troubleshooting

If you have issues with running from the context menu, you can open up a command prompt and run the captioner manually:

`C:\EasyCaption\EasyCaption.exe "path_to_your_image_file.png"`

This will show you any error messages in the console.
