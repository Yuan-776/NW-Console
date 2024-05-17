using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using NLog;
using NW_Console.Model;

namespace NW_Console
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = Directory.GetCurrentDirectory() + "//nlog.config";

            var logger = LogManager.LoadConfiguration(path).GetCurrentClassLogger();
            logger.Info("Program started");

            try
            {
                var db = new NWContext();

                string choice;
                do
                {
                    Console.WriteLine("1) Display Categories");
                    Console.WriteLine("2) Add Category");
                    Console.WriteLine("3) Display Category and related products");
                    Console.WriteLine("4) Display all Categories and their related products");
                    Console.WriteLine("5) Add Product");
                    Console.WriteLine("6) Edit Product");
                    Console.WriteLine("7) Display Products");
                    Console.WriteLine("8) Display a Specific Product");
                    Console.WriteLine("9) Edit Category");
                    Console.WriteLine("10) Delete Product");
                    Console.WriteLine("11) Delete Category");

                    Console.WriteLine("\"q\" to quit");
                    choice = Console.ReadLine();
                    Console.Clear();
                    logger.Info($"Option {choice} selected");

                    switch (choice)
                    {
                        case "1":
                            DisplayCategories(db);
                            break;
                        case "2":
                            AddCategory(db, logger);
                            break;
                        case "3":
                            DisplayCategoryAndProducts(db, logger);
                            break;
                        case "4":
                            DisplayAllCategoriesAndProducts(db);
                            break;
                        case "5":
                            AddProduct(db, logger);
                            break;
                        case "6":
                            EditProduct(db, logger);
                            break;
                        case "7":
                            DisplayProducts(db);
                            break;
                        case "8":
                            DisplaySpecificProduct(db, logger);
                            break;
                        case "9":
                            EditCategory(db, logger);
                            break;
                        case "10":
                            DeleteProduct(db, logger);
                            break;
                        case "11":
                            DeleteCategory(db, logger);
                            break;
                        default:
                            if (choice.ToLower() != "q")
                            {
                                Console.WriteLine("Invalid choice");
                            }
                            break;
                    }
                } while (choice.ToLower() != "q");
            }
            catch (Exception ex)
            {
                logger.Error(ex, ex.Message);
                if (ex.InnerException != null)
                {
                    logger.Error(ex.InnerException, ex.InnerException.Message);
                }
            }
            logger.Info("Program ended");
        }

        static void DisplayCategories(NWContext db)
        {
            var query = db.Categories.OrderBy(p => p.CategoryName);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{query.Count()} records returned");
            Console.ForegroundColor = ConsoleColor.Magenta;
            foreach (var item in query)
            {
                Console.WriteLine($"{item.CategoryName} - {item.Description}");
            }
            Console.ForegroundColor = ConsoleColor.White;
        }

        static void AddCategory(NWContext db, Logger logger)
        {
            Category category = new Category();
            Console.WriteLine("Enter Category Name:");
            category.CategoryName = Console.ReadLine();
            Console.WriteLine("Enter the Category Description:");
            category.Description = Console.ReadLine();
            ValidationContext context = new ValidationContext(category, null, null);
            List<ValidationResult> results = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(category, context, results, true);
            if (isValid)
            {
                // check for unique name
                if (db.Categories.Any(c => c.CategoryName == category.CategoryName))
                {
                    // generate validation error
                    isValid = false;
                    results.Add(new ValidationResult("Name exists", new string[] { "CategoryName" }));
                }
                else
                {
                    logger.Info("Validation passed");
                    db.Categories.Add(category);
                    db.SaveChanges();
                    logger.Info("Category added: " + category.CategoryName);
                }
            }
            if (!isValid)
            {
                foreach (var result in results)
                {
                    logger.Error($"{result.MemberNames.First()} : {result.ErrorMessage}");
                }
            }
        }

        static void DisplayCategoryAndProducts(NWContext db, Logger logger)
        {
            var query = db.Categories.OrderBy(p => p.CategoryId);
            Console.WriteLine("Select the category whose products you want to display:");
            Console.ForegroundColor = ConsoleColor.DarkRed;
            foreach (var item in query)
            {
                Console.WriteLine($"{item.CategoryId}) {item.CategoryName}");
            }
            Console.ForegroundColor = ConsoleColor.White;
            int id = int.Parse(Console.ReadLine());
            Console.Clear();
            logger.Info($"CategoryId {id} selected");
            Category category = db.Categories.Include(c => c.Products).FirstOrDefault(c => c.CategoryId == id);
            if (category != null)
            {
                Console.WriteLine($"{category.CategoryName} - {category.Description}");
                foreach (Product p in category.Products)
                {
                    Console.WriteLine($"\t{p.ProductName}");
                }
            }
            else
            {
                Console.WriteLine("Category not found.");
            }
        }

        static void DisplayAllCategoriesAndProducts(NWContext db)
        {
            var query = db.Categories.Include(c => c.Products).OrderBy(p => p.CategoryId);
            foreach (var item in query)
            {
                Console.WriteLine($"{item.CategoryName}");
                foreach (Product p in item.Products)
                {
                    Console.WriteLine($"\t{p.ProductName}");
                }
            }
        }

        static void AddProduct(NWContext db, Logger logger)
        {
            Product newProduct = new Product();
            Console.WriteLine("Enter Product Name:");
            newProduct.ProductName = Console.ReadLine();
            Console.WriteLine("Enter Supplier ID:");
            newProduct.SupplierId = int.Parse(Console.ReadLine());
            Console.WriteLine("Enter Category ID:");
            newProduct.CategoryId = int.Parse(Console.ReadLine());
            Console.WriteLine("Enter Quantity Per Unit:");
            newProduct.QuantityPerUnit = Console.ReadLine();
            Console.WriteLine("Enter Unit Price:");
            newProduct.UnitPrice = decimal.Parse(Console.ReadLine());
            Console.WriteLine("Enter Units in Stock:");
            newProduct.UnitsInStock = short.Parse(Console.ReadLine());
            Console.WriteLine("Enter Units on Order:");
            newProduct.UnitsOnOrder = short.Parse(Console.ReadLine());
            Console.WriteLine("Enter Reorder Level:");
            newProduct.ReorderLevel = short.Parse(Console.ReadLine());
            Console.WriteLine("Is it discontinued? (true/false):");
            newProduct.Discontinued = bool.Parse(Console.ReadLine());

            try
            {
                db.Products.Add(newProduct);
                db.SaveChanges();
                logger.Info("New product added: " + newProduct.ProductName);
            }
            catch (Exception ex)
            {
                logger.Error(ex, ex.Message);
                if (ex.InnerException != null)
                {
                    logger.Error(ex.InnerException, ex.InnerException.Message);
                }
            }
        }

        static void EditProduct(NWContext db, Logger logger)
        {
            Console.WriteLine("Enter Product ID to edit:");
            int productId = int.Parse(Console.ReadLine());
            var product = db.Products.Find(productId);
            if (product != null)
            {
                Console.WriteLine("Enter new Product Name:");
                product.ProductName = Console.ReadLine();
                try
                {
                    db.SaveChanges();
                    logger.Info("Product updated: " + product.ProductName);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, ex.Message);
                    if (ex.InnerException != null)
                    {
                        logger.Error(ex.InnerException, ex.InnerException.Message);
                    }
                }
            }
            else
            {
                Console.WriteLine("Product not found.");
                logger.Error("Product not found: " + productId);
            }
        }

        static void DisplayProducts(NWContext db)
        {
            Console.WriteLine("Choose to display: 'all', 'active', or 'discontinued' products:");
            string filter = Console.ReadLine().ToLower();
            IQueryable<Product> products;
            switch (filter)
            {
                case "all":
                    products = db.Products;
                    break;
                case "active":
                    products = db.Products.Where(p => !p.Discontinued);
                    break;
                case "discontinued":
                    products = db.Products.Where(p => p.Discontinued);
                    break;
                default:
                    Console.WriteLine("Invalid choice");
                    return;
            }

            foreach (var p in products)
            {
                Console.WriteLine(p.ProductName + (p.Discontinued ? " - Discontinued" : " - Active"));
            }
        }
        static void DisplaySpecificProduct(NWContext db, Logger logger)
        {
            Console.WriteLine("Enter Product ID to display:");
            int productId = int.Parse(Console.ReadLine());
            var product = db.Products.Find(productId);
            if (product != null)
            {
                Console.WriteLine($"Product Name: {product.ProductName}, Price: {product.UnitPrice}, Stock: {product.UnitsInStock}, Discontinued: {product.Discontinued}");
            }
            else
            {
                Console.WriteLine("Product not found.");
                logger.Error("Product not found: " + productId);
            }
        }

        static void EditCategory(NWContext db, Logger logger)
        {
            Console.WriteLine("Enter Category ID to edit:");
            int categoryId = int.Parse(Console.ReadLine());
            var category = db.Categories.Find(categoryId);
            if (category != null)
            {
                Console.WriteLine("Enter new Category Name:");
                category.CategoryName = Console.ReadLine();
                Console.WriteLine("Enter new Description:");
                category.Description = Console.ReadLine();
                try
                {
                    db.SaveChanges();
                    logger.Info("Category updated: " + category.CategoryName);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to update category: " + ex.Message);
                    if (ex.InnerException != null)
                    {
                        logger.Error(ex.InnerException, ex.InnerException.Message);
                    }
                }
            }
            else
            {
                Console.WriteLine("Category not found.");
                logger.Error("Category not found: " + categoryId);
            }
        }

        static void DeleteProduct(NWContext db, Logger logger)
        {
            Console.WriteLine("Enter Product ID to delete:");
            int productId = int.Parse(Console.ReadLine());
            var product = db.Products.Find(productId);
            if (product != null)
            {
                try
                {
                    db.Products.Remove(product);
                    db.SaveChanges();
                    logger.Info("Product deleted: " + productId);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to delete product: " + ex.Message);
                    if (ex.InnerException != null)
                    {
                        logger.Error(ex.InnerException, ex.InnerException.Message);
                    }
                }
            }
            else
            {
                Console.WriteLine("Product not found.");
                logger.Error("Product not found: " + productId);
            }
        }

        static void DeleteCategory(NWContext db, Logger logger)
        {
            Console.WriteLine("Enter Category ID to delete:");
            int categoryId = int.Parse(Console.ReadLine());
            var category = db.Categories.Include(c => c.Products).FirstOrDefault(c => c.CategoryId == categoryId);
            if (category != null)
            {
                if (!category.Products.Any())
                {
                    try
                    {
                        db.Categories.Remove(category);
                        db.SaveChanges();
                        logger.Info("Category deleted: " + categoryId);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Failed to delete category: " + ex.Message);
                        if (ex.InnerException != null)
                        {
                            logger.Error(ex.InnerException, ex.InnerException.Message);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Category cannot be deleted, it has related products.");
                    logger.Error("Category has related products and cannot be deleted: " + categoryId);
                }
            }
            else
            {
                Console.WriteLine("Category not found.");
                logger.Error("Category not found: " + categoryId);
            }
        }
    }
}
