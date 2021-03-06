﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BookStore.Application;
using BookStore.Domain.Application;
using BookStore.Domain.Models;
using BookStore.Repositories;
using Microsoft.AspNet.SignalR;

namespace BookStore.Controllers
{
    public class BooksController : Controller
    {
        private IStorageHelper _storageHelper;

        public BooksController(IStorageHelper storageHelper)
        {
            _storageHelper = storageHelper;
        }

        // GET: Books
        public ActionResult Index()
        {
            OperationResult<IEnumerable<Book>> result = null;

            using (var db = new BookStoreDb())
            {
                var app = new BookStoreApp(db);
                result = app.GetBooks();
            }

            if (!result.Success)
            {
                // display the error message if you wish
                return View("Error");
            }

            return View(result.Result);
        }

        // GET: Books/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Books/Create
        [System.Web.Mvc.Authorize(Roles = "Employee,Admin")]
        public ActionResult Create()
        {
            var publishers = Enumerable.Empty<Publisher>();
            using (var db = new BookStoreDb())
            {
                publishers = db.Publishers.All();
            }

            ViewBag.PublisherId = new SelectList(publishers, "Id", "Name");
            
            return View();
        }

        // POST: Books/Create
        [System.Web.Mvc.Authorize(Roles = "Employee,Admin")]
        [HttpPost]
        public ActionResult Create([Bind(Include = "Title,Author,PublicationYear")]Book book, int publisherId, HttpPostedFileBase CoverPhoto)
        {
            try
            {
                if (CoverPhoto != null)
                {
                    var imageFileName = Path.GetFileName(CoverPhoto.FileName);
                    var imageContentType = CoverPhoto.ContentType;
                    var folderPath = Server.MapPath("~/Content/Photos");
                    var uploaded = _storageHelper.UploadImage(imageFileName, imageContentType,
                        CoverPhoto.InputStream, folderPath);
                    if (uploaded.Success)
                    {
                        book.CoverPhoto = uploaded.Result;
                    }
                }
                else
                {
                    book.CoverPhoto = "noimage.png";
                    // alternatively don't allow saving if a photo is not uploaded
                    //ModelState.AddModelError("CoverPhoto", "No cover image uploaded");
                    //return View(book);
                }

                using (var db = new BookStoreDb())
                {
                    var publisher = db.Publishers.Find(publisherId);
                    book.Publisher = publisher;
                    db.Books.Create(book);
                }

                var hubContext = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();
                hubContext?.Clients?.All?.onBookAdded(book);
                
                return RedirectToAction("Index");
            }
            catch(Exception e)
            {
                ModelState.AddModelError("create", e);

                using (var db = new BookStoreDb())
                {
                    ViewBag.PublisherId = new SelectList(db.Publishers.All(), "Id", "Name");
                }

                return View(book);
            }
        }

        // GET: Books/Edit/5
        [System.Web.Mvc.Authorize(Roles = "Employee,Admin")]
        public ActionResult Edit(int id)
        {
            var publishers = Enumerable.Empty<Publisher>();
            Book book = null;
            using (var db = new BookStoreDb())
            {
                publishers = db.Publishers.All();
                book = db.Books.Find(id);
            }

            ViewBag.PublisherId = new SelectList(publishers, "Id", "Name");

            return View(book);
        }

        // POST: Books/Edit/5
        [System.Web.Mvc.Authorize(Roles = "Employee,Admin")]
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Books/Delete/5
        [System.Web.Mvc.Authorize(Roles = "Admin")]
        public ActionResult Delete(int id)
        {
            Book book = null;
            using (var db = new BookStoreDb())
            {
                book = db.Books.Find(id);
            }

            if (book == null)
            {
                return HttpNotFound("Book not found");
            }

            return View(book);
        }

        // POST: Books/Delete/5
        [System.Web.Mvc.Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                using (var db = new BookStoreDb())
                {
                    db.Books.Delete(id);
                }

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
