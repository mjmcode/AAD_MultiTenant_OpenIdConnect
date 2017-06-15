using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TodoListWebApp.Models;
using System.Security.Claims;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using Microsoft.Owin.Security;
using System.Net.Http.Headers;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Configuration;

namespace TodoListWebApp.Controllers
{
    // claims-aware controller for CRUD operations on the Todo collection
    [Authorize]
    public class TodoController : Controller
    {
        private string todoListResourceId = ConfigurationManager.AppSettings["todo:TodoListResourceId"];
        private string todoListBaseAddress = ConfigurationManager.AppSettings["todo:TodoListBaseAddress"];
        private static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private static string appKey = ConfigurationManager.AppSettings["ida:Password"];
        Uri redirectUri = new Uri(ConfigurationManager.AppSettings["ida:RedirectUri"]);


        public async Task<ActionResult> Index()
        {
            AuthenticationResult result = null;
            List<Todo> itemList = new List<Todo>();

            try
            {
                string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
                AuthenticationContext authContext = new AuthenticationContext(Startup.Authority, false);
                ClientCredential credential = new ClientCredential(clientId, appKey);
                UserIdentifier identifier = new UserIdentifier(userObjectID, UserIdentifierType.UniqueId);
                ////var test_result = await authContext.AcquireTokenSilentAsync(todoListResourceId, credential, identifier);

                result = authContext.AcquireToken(todoListResourceId, clientId, redirectUri, PromptBehavior.Never, identifier);
                //result = await authContext.AcquireTokenSilentAsync(todoListResourceId, credential, identifier);
                //result = await authContext.AcquireTokenAsync(todoListResourceId, credential);

                //
                // Retrieve the user's To Do List.
                //
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, todoListBaseAddress + "/api/todolist");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                HttpResponseMessage response = await client.SendAsync(request);

                //
                // Return the To Do List in the view.
                //
                if (response.IsSuccessStatusCode)
                {
                    List<Dictionary<String, String>> responseElements = new List<Dictionary<String, String>>();
                    JsonSerializerSettings settings = new JsonSerializerSettings();
                    String responseString = await response.Content.ReadAsStringAsync();
                    responseElements = JsonConvert.DeserializeObject<List<Dictionary<String, String>>>(responseString, settings);
                    foreach (Dictionary<String, String> responseElement in responseElements)
                    {
                        Todo newItem = new Todo();
                        newItem.Description = responseElement["Title"];
                        newItem.Owner = responseElement["Owner"];
                        itemList.Add(newItem);
                    }

                    return View(itemList);
                }
                else
                {
                    //
                    // If the call failed with access denied, then drop the current access token from the cache, 
                    //     and show the user an error indicating they might need to sign-in again.
                    //
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        var todoTokens = authContext.TokenCache.ReadItems().Where(a => a.Resource == todoListResourceId);
                        foreach (TokenCacheItem tci in todoTokens)
                            authContext.TokenCache.DeleteItem(tci);

                        ViewBag.ErrorMessage = "UnexpectedError";
                        Todo newItem = new Todo();
                        newItem.Description = "(No items in list)";
                        itemList.Add(newItem);
                        return View(itemList);
                    }
                }
            }
            catch (AdalException ee)
            {
                if (Request.QueryString["reauth"] == "True")
                {
                    //
                    // Send an OpenID Connect sign-in request to get a new set of tokens.
                    // If the user still has a valid session with Azure AD, they will not be prompted for their credentials.
                    // The OpenID Connect middleware will return to this controller after the sign-in response has been handled.
                    //
                    HttpContext.GetOwinContext().Authentication.Challenge(
                        new AuthenticationProperties(),
                        OpenIdConnectAuthenticationDefaults.AuthenticationType);
                }

                //
                // The user needs to re-authorize.  Show them a message to that effect.
                //
                Todo newItem = new Todo();
                newItem.Description = "(Sign-in required to view to do list.)";
                itemList.Add(newItem);
                ViewBag.ErrorMessage = "AuthorizationRequired";
                return View(itemList);
            }
            catch (Exception ex)
            {
                //
            }

            //
            // If the call failed for any other reason, show the user an error.
            //

            return View("Error");

            //}
        }



        //// GET: /Todo/
        //public ActionResult Index()
        //{
        //    string owner = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
        //    var currentUserToDos = db.Todoes.Where(a => a.Owner == owner);
        //    return View(currentUserToDos.ToList());
        //}

        //// GET: /Todo/Details/5
        //public ActionResult Details(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    Todo todo = db.Todoes.Find(id);
        //    string owner = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
        //    if (todo == null || (todo.Owner != owner))
        //    {
        //        return HttpNotFound();
        //    }
        //    return View(todo);
        //}

        //// GET: /Todo/Create
        //public ActionResult Create()
        //{
        //    return View();
        //}

        //// POST: /Todo/Create
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Create([Bind(Include = "ID,Description")] Todo todo)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        todo.Owner = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
        //        db.Todoes.Add(todo);
        //        db.SaveChanges();
        //        return RedirectToAction("Index");
        //    }

        //    return View(todo);
        //}
        //// GET: /Todo/Edit/5
        //public ActionResult Edit(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    Todo todo = db.Todoes.Find(id);
        //    string owner = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
        //    if (todo == null || (todo.Owner != owner))
        //    {
        //        return HttpNotFound();
        //    }
        //    return View(todo);
        //}

        //// POST: /Todo/Edit/5
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Edit([Bind(Include = "ID,Description")] Todo todo)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        db.Entry(todo).State = EntityState.Modified;
        //        db.SaveChanges();
        //        return RedirectToAction("Index");
        //    }
        //    return View(todo);
        //}

        //// GET: /Todo/Delete/5
        //public ActionResult Delete(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    Todo todo = db.Todoes.Find(id);
        //    string owner = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
        //    if (todo == null || (todo.Owner != owner))
        //    {
        //        return HttpNotFound();
        //    }
        //    return View(todo);
        //}

        //// POST: /Todo/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public ActionResult DeleteConfirmed(int id)
        //{
        //    Todo todo = db.Todoes.Find(id);
        //    string owner = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
        //    if (todo == null || (todo.Owner != owner))
        //    {
        //        return HttpNotFound();
        //    }
        //    db.Todoes.Remove(todo);
        //    db.SaveChanges();
        //    return RedirectToAction("Index");
        //}

        //protected override void Dispose(bool disposing)
        //{
        //    if (disposing)
        //    {
        //        db.Dispose();
        //    }
        //    base.Dispose(disposing);
        //}
    }
}
