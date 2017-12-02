#Adding docker support to Miniblog.Core

##Summary
This article demonstrates how to a possible way of adding docker support to Miniblog.Core. On the second part of this article I will demonstrate how you could deploy this application to a Kubernetes clusters on Azure using Persistent Volumes for application state.
I assume that you are familiar with Docker, Docker-Compose and Kubernetes.



## What is Miniblog.Core
Miniblog is a nice and simple blog engine based on .NET core. The default implementation relies on the file system to store the blog entries. 
Source code can be found here:

## The plan
Applications in containers are stateless by default. If they need to be restarted they will lose all their state, unless you take a action to save it outside the container.
We need to solve this problem when adding docker support. Other idea is that currently the deployment model desired by the author of the project is that you fork it, make your customizations and deploy it in your host of your choice. What I would like to achieve is that at the end of the process I can provide a single docker image and customize the application based on environment variables that can be passed to the application.

The todo-list is:
- Add docker support to the project (a simple click on Visual Studio)
- Using Docker-Compose mount the file system for the blog data outside the container
- Refactor all dependencies on static assets to use the provided environment variables
  
## Add docker support
This is easy on Visual Studio 2017. If you have one of the newest versions you just need to click on menu "Project" and then "Add Docker Support".
This will create a docker-compose project on your solution, as well as adding a "Dockerfile" file to the Miniblog.Core project.

## Storing blog data out of the container
To store the blog data out of the container we need to mount a directory in the hosting OS to the container, this way the data stored will persist even if the container dies.

First we need to mount a volume in docker-compose:
```
services:
  miniblog.core:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
    ports:
      - "80"
    #using volumes "path in host os":"path in container"
    volumes: 
      # windows version: make sure the folder d:/dev/projects/Miniblog.Core/miniblog-datavolume exists on your windows environment
      - d:/dev/projects/Miniblog.Core/miniblog-datavolume:/app/data
```

This will bind the path app/data inside the container to the folder d:/dev/projects/Miniblog.Core/miniblog-datavolume in your local Windows OS.
The next change we need to do is in the FileBlogService where we need to verify if the path /app/data exists before assuming the default.
```
public FileBlogService(IHostingEnvironment env, IHttpContextAccessor contextAccessor)
{
    // We will mount /app/data in docker/k8s
    var mountedPath = "/app/data";
    if (Directory.Exists(mountedPath))
        _folder = Path.Combine(mountedPath, "Posts");
    else
        _folder = Path.Combine(env.WebRootPath, "Posts");
    _contextAccessor = contextAccessor;

    Initialize();
}
```

#Removing the dependency to static assets
Miniblog.Core uses a few static files that must be modified by anyone who wants to use the blog engine.
The following static assets should resolve the information outside the container:
- Web manifest (provided in file wwwroot/manifest.json)
- User login (defined in appsettings.json)

##Replacing the static web manifest by a dynamic version
The web manifest static file is resolved by the call to services.AddProgressiveWebApp(...) defined in the package https://github.com/madskristensen/WebEssentials.AspNetCore.ServiceWorker from the same author.
A possible solution would be to resolve the manifest customizable values from the environment variables and pass it to pass it to the AddProgressiveWebApp,

 