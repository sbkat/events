﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using event_scheduler.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace event_scheduler.Controllers
{
    public class HomeController : Controller
    {
    private MyContext dbContext;
    public HomeController(MyContext context)
    {
        dbContext = context;
    }
    [HttpGet("")]
    public IActionResult Index()
    {
        List<PublicEvent> allEvents = dbContext.Events
            .Include(e => e.Creator)
            .Include(e => e.Participants)
            .ThenInclude(participant => participant.Attending)
            .OrderBy(w => w.Date).ToList();
        return View(allEvents);
    }
    [HttpGet("register")]
    public IActionResult RegisterPartial()
    {
        return View();
    }
    [HttpPost("register")]
    public IActionResult Register(User newUser)
    {
        if(ModelState.IsValid)
        {
            if(dbContext.Users.Any(user => user.email == newUser.email))
            {
                ModelState.AddModelError("email", "Email is already registered.");
                return View("Login");
            }
            else
            {
                PasswordHasher<User> Hasher = new PasswordHasher<User>();
                newUser.password = Hasher.HashPassword(newUser, newUser.password);
                dbContext.Users.Add(newUser);
                dbContext.SaveChanges();
                HttpContext.Session.SetString("User", newUser.email);
                HttpContext.Session.SetString("UserName", newUser.firstName);
                HttpContext.Session.SetInt32("UserId", newUser.UserId);
                return RedirectToAction("Dashboard");
            }
        }
        else
        {
            return View("Login");
        }
    }
    [HttpGet("login")]
    public IActionResult Login()
    {
        return View();
    }
    [HttpPost("login")]
    public IActionResult Login(LoginUser existingUser)
    {
        if(ModelState.IsValid)
        {
            if(dbContext.Users.Any(user => user.email == existingUser.loginemail))
            {
                User userInDb = dbContext.Users.FirstOrDefault(user => user.email == existingUser.loginemail);
                var hasher = new PasswordHasher<LoginUser>();
                var result = hasher.VerifyHashedPassword(existingUser, userInDb.password, existingUser.loginpassword);
                if(result == 0)
                {
                    ModelState.AddModelError("loginemail", "Invalid Email/Password");
                    return View("Login");
                }
                else
                {
                    HttpContext.Session.SetString("User", existingUser.loginemail);
                    HttpContext.Session.SetString("UserName", userInDb.firstName);
                    HttpContext.Session.SetInt32("UserId", userInDb.UserId);
                    return RedirectToAction("Dashboard");
                }
            }
            else
            {
                ModelState.AddModelError("loginemail", "Email has not been registered.");
                return View("Login");
            }
        }
        else
        {
            return View("Login");
        }
    }
    [HttpGet("events")]
    public IActionResult Dashboard() 
    {
        List<PublicEvent> allEvents = dbContext.Events
            .Include(e => e.Creator)
            .Include(e => e.Participants)
            .ThenInclude(participant => participant.Attending)
            .OrderBy(w => w.Date).ToList();
        return View(allEvents);
    }
    [HttpGet("events/new")]
    public IActionResult New()
    {        
        if(HttpContext.Session.GetString("User")==null)
        {
            return RedirectToAction("LoginError");
        }
        else
        {
        return View();
        }
    }
    [HttpPost("events")]
    public IActionResult SubmitNew(Participant newEvent)
    {
        if(ModelState.IsValid)
        {
            if(newEvent.Event.Date > DateTime.Now)
            {
                User thisUser = dbContext.Users.FirstOrDefault(user => user.UserId == HttpContext.Session.GetInt32("UserId"));
                newEvent.Event.Creator = thisUser;
                newEvent.Event.UserId = thisUser.UserId;
                dbContext.Add(newEvent.Event);
                dbContext.SaveChanges();
                return RedirectToAction("Dashboard");
            }
            else
            {
                ModelState.AddModelError("Date", "Event date must be chosen for the future.");
                return View("New");
            }
        }
        return View("New");
    }
    [HttpGet("events/{id}")]
    public IActionResult Details(int id)
    {
        PublicEvent thisEvent = dbContext.Events
            .Include(e => e.Creator)
            .Include(e => e.Participants)
            .ThenInclude(p => p.Attending)
            .FirstOrDefault(e => e.EventId == id);
        return View(thisEvent);
    }
    [HttpGet("events/{id}/edit")]
    public IActionResult Edit(int id)
    {
        if(HttpContext.Session.GetString("User")==null)
        {
            return RedirectToAction("Index");
        }
        else
        {
            PublicEvent thisEvent = dbContext.Events
                .Include(e => e.Creator)
                .Include(e => e.Participants)
                .ThenInclude(p => p.Attending)
                .FirstOrDefault(e => e.EventId == id);
            return View(thisEvent);
        }
    }
    [HttpPost("events/{id}")]
    public IActionResult Update(PublicEvent editEvent, int id)
    {
        if(ModelState.IsValid)
        {
            PublicEvent thisEvent = dbContext.Events.FirstOrDefault(e => e.EventId == id);
            thisEvent.Title = editEvent.Title;
            thisEvent.Time = editEvent.Time;
            thisEvent.Date = editEvent.Date;
            thisEvent.DurationInt = editEvent.DurationInt;
            thisEvent.DurationTime = editEvent.DurationTime;
            thisEvent.Description = editEvent.Description;
            thisEvent.UpdatedAt = DateTime.Now;
            thisEvent.UserId = (int)HttpContext.Session.GetInt32("UserId");
            dbContext.Update(thisEvent);
            dbContext.SaveChanges();
            return RedirectToAction("Dashboard");
        }
        else
        {
            return View("Edit", id);
        }
    }
    [HttpPost("RSVP")]
    public IActionResult RSVP(int id) 
    {
        if(HttpContext.Session.GetString("User")==null)
        {
            return RedirectToAction("LoginError");
        }
        Participant UserRsvp = dbContext.Participants.Where(act => act.EventId == id).FirstOrDefault(user => user.UserId == HttpContext.Session.GetInt32("UserId"));
        User thisUser = dbContext.Users.FirstOrDefault(user => user.UserId == HttpContext.Session.GetInt32("UserId")); 
        if(UserRsvp==null)
        {
            Participant newRSVP = new Participant();
            newRSVP.EventId = id;
            newRSVP.UserId = (int)HttpContext.Session.GetInt32("UserId");
            newRSVP.Attending = thisUser;
            dbContext.Add(newRSVP);
            dbContext.SaveChanges();
            return RedirectToAction("Dashboard");
        }
        else
        {
            dbContext.Participants.Remove(UserRsvp);
            dbContext.SaveChanges();
            return RedirectToAction("Dashboard");
        }
    }
    [HttpGet("error")]
    public IActionResult LoginError()
    {
        return View();
    }
    [HttpPost("events/{id}/delete")]
    public IActionResult Delete(int id)
    {
        PublicEvent thisEvent = dbContext.Events.FirstOrDefault(e => e.EventId == id);
        dbContext.Events.Remove(thisEvent);
        dbContext.SaveChanges();
        return RedirectToAction("Dashboard");
    }
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index");
    }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
