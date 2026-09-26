using System;
using ExpenseHub.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseHub.Api.Controllers;

internal static class ServiceResultExtensions
{
    public static IActionResult ToActionResult<T>(
        this ControllerBase controller,
        ServiceResult<T> result,
        Func<T, IActionResult> onSuccess)
        where T : notnull
    {
        return result.Succeeded ? onSuccess(result.Value) : controller.ToProblem(result.Error);
    }

    // um lugar só decidindo o status de cada erro de negócio
    public static IActionResult ToProblem(this ControllerBase controller, ServiceError error)
    {
        if (error.Kind == ServiceErrorKind.Validation)
        {
            controller.ModelState.AddModelError(error.Field ?? string.Empty, error.Message);
            return controller.ValidationProblem(controller.ModelState);
        }

        (int status, string title) = error.Kind switch
        {
            ServiceErrorKind.NotFound => (StatusCodes.Status404NotFound, "Recurso não encontrado."),
            ServiceErrorKind.Forbidden => (StatusCodes.Status403Forbidden, "Operação não permitida."),
            ServiceErrorKind.Conflict => (StatusCodes.Status409Conflict, "Conflito com o estado atual."),
            _ => (StatusCodes.Status400BadRequest, "Requisição inválida."),
        };

        return controller.Problem(detail: error.Message, statusCode: status, title: title);
    }
}
