open System
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection

module Models =
    open Microsoft.EntityFrameworkCore
    type ToDoDto = {Description: string}

    [<AllowNullLiteral>]
    type ToDo(idIn: string, descriptionIn: string) = 
        let mutable id = idIn
        let mutable description = descriptionIn
        member this.Id with get() = id and set(v) = id <- v
        member this.Description with get() = description and set(v) = description <- v
        new() = ToDo(Guid.NewGuid().ToString(),"sampleDescription")

    type DefaultResponse = {IsSuccessful: bool; Message: string}
    
    type DeleteToDoDto = {Id: string}

    type ToDoDbContext(options : DbContextOptions<ToDoDbContext> ) = 
        inherit DbContext(options)
        [<DefaultValue>]
            val mutable ToDoItems : DbSet<ToDo>
            member public this._ToDoItems with get() = this.ToDoItems and set value = this.ToDoItems <- value

open Models
open Microsoft.EntityFrameworkCore
open System.Text.Json
open Microsoft.AspNetCore.Mvc
open System.Net

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    
    builder.Host.ConfigureServices(fun services -> 
        services.AddCors() |> ignore
        services.AddDbContext<ToDoDbContext>(Action<DbContextOptionsBuilder>(fun opt -> opt.UseInMemoryDatabase("ToDoList") |> ignore)) 
        |> ignore) 
        |> ignore

    let app = builder.Build()

    app.UseCors(fun builder -> builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader() |> ignore) |> ignore

    app.MapGet("/", Func<string>(fun () -> "Hello World!")) |> ignore

    app.MapGet("/getToDos", Func<ToDoDbContext,ToDo list>(fun context -> 
        context.ToDoItems |> Seq.toList
        )) |> ignore

    app.MapPost("/createToDo", Func<ToDoDto,ToDoDbContext,ActionResult<ToDo>>(fun todo context ->
            let identifier = Guid.NewGuid().ToString()    
            let todo = new ToDo(identifier, todo.Description.ToUpper())
            context.ToDoItems.Add(todo) |> ignore
            context.SaveChanges() |> ignore
            ActionResult<ToDo>(todo)
        ) ) |> ignore

    app.MapPost("/deleteToDo", Func<DeleteToDoDto, ToDoDbContext, ActionResult<DefaultResponse>>(fun deleteToDoDto context ->
        match context.ToDoItems.Find(deleteToDoDto.Id) with
        | null -> ActionResult<DefaultResponse>({Message= "Task not found"; IsSuccessful= false})
        | todo->
                context.Remove(todo) |> ignore
                context.SaveChanges() |> ignore
                ActionResult<DefaultResponse>({Message= "OK"; IsSuccessful= true})

    )) |> ignore

    app.Run()

    0 // Exit code

