﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;

using NUnit.Framework;

namespace Data.Linq
{
	using Model;

	[TestFixture]
	public class SelectManyTest : TestBase
	{
		[Test]
		public void Test1()
		{
			TestJohn(db =>
			{
				var q = db.Person.Select(p => p);

				return db.Person
					.SelectMany(p1 => q/*db.Person.Select(p => p)*/, (p1, p2) => new { p1, p2 })
					.Where(t => t.p1.ID == t.p2.ID && t.p1.ID == 1)
					.Select(t => new Person { ID = t.p1.ID, FirstName = t.p2.FirstName });
			});
		}

		[Test]
		public void Test21()
		{
			TestJohn(db =>
				from p1 in from p in db.Person select new { ID1 = p.ID, p.LastName  }
				from p2 in from p in db.Person select new { ID2 = p.ID, p.FirstName }
				from p3 in from p in db.Person select new { ID3 = p.ID, p.LastName  }
				where p1.ID1 == p2.ID2 && p1.LastName == p3.LastName && p1.ID1 == 1
				select new Person { ID = p1.ID1, FirstName = p2.FirstName, LastName = p3.LastName } );
		}

		[Test]
		public void Test22()
		{
			TestJohn(db =>
				from p1 in from p in db.Person select p
				from p2 in from p in db.Person select p
				from p3 in from p in db.Person select p
				where p1.ID == p2.ID && p1.LastName == p3.LastName && p1.ID == 1
				select new Person { ID = p1.ID, FirstName = p2.FirstName, LastName = p3.LastName } );
		}

		[Test]
		public void Test31()
		{
			TestJohn(db =>
				from p in
					from p in
						from p in db.Person
						where p.ID == 1
						select new { p, ID = p.ID + 1 }
					where p.ID == 2
					select new { p, ID = p.ID + 1 }
				where p.ID == 3
				select p.p.p);
		}

		[Test]
		public void Test32()
		{
			ForEachProvider(db =>
			{
				var q =
					from p in
						from p in
							from p in db.Person
							where p.ID == 1
							select new { p, ID = p.ID + 1 }
						where p.ID == 2
						select new { p, ID = p.ID + 1 }
					where p.ID == 3
					select new { p.p.p };

				var list = q.ToList();

				Assert.AreEqual(1, list.Count);

				var person = list[0].p;

				Assert.AreEqual(1,      person.ID);
				Assert.AreEqual("John", person.FirstName);
			});
		}

		[Test]
		public void SubQuery1()
		{
			TestJohn(db =>
			{
				var id = 1;
				var q  = from p in db.Person where p.ID == id select p;

				return 
					from p1 in db.Person
					from p2 in q
					where p1.ID == p2.ID
					select new Person { ID = p1.ID, FirstName = p2.FirstName };
			});
		}

		public void SubQuery2(TestDbManager db)
		{
			var q1 = from p in db.Person where p.ID == 1 || p.ID == 2 select p;
			var q2 = from p in db.Person where !(p.ID == 2) select p;

			var q = 
				from p1 in q1
				from p2 in q2
				where p1.ID == p2.ID
				select new Person { ID = p1.ID, FirstName = p2.FirstName };

			foreach (var person in q)
			{
				Assert.AreEqual(1,      person.ID);
				Assert.AreEqual("John", person.FirstName);
			}
		}

		[Test]
		public void SubQuery2()
		{
			ForEachProvider(db =>
			{
				SubQuery2(db);
				SubQuery2(db);
			});
		}

		IQueryable<Person> GetPersonQuery(TestDbManager db, int id)
		{
			return from p in db.Person where p.ID == id select new Person { ID = p.ID + 1, FirstName = p.FirstName };
		}

		[Test]
		public void SubQuery3()
		{
			TestJohn(db =>
			{
				var q = GetPersonQuery(db, 1);

				return
					from p1 in db.Person
					from p2 in q
					where p1.ID == p2.ID - 1
					select new Person { ID = p1.ID, FirstName = p2.FirstName };
			});
		}

		[Test]
		public void OneParam()
		{
			TestJohn(db => db.Person.SelectMany(p => db.Person).Where(t => t.ID == 1).Select(t => t));
		}

		[Test]
		public void ScalarQuery()
		{
			TestJohn(db =>
				from p1 in db.Person
				from p2 in (from p in db.Person select p.ID)
				where p1.ID == p2
				select new Person { ID = p2, FirstName = p1.FirstName }
			);
		}

		[Test]
		public void SelectManyLeftJoin()
		{
			var expected =
				from p in Parent
				from c in p.Children.Select(o => new { o.ChildID, p.ParentID }).DefaultIfEmpty()
				select new { p.Value1, o = c };

			ForEachProvider(db => Assert.AreEqual(expected.Count(),
				(from p in db.Parent
				from c in p.Children.Select(o => new { o.ChildID, p.ParentID }).DefaultIfEmpty()
				select new { p.Value1, o = c }).AsEnumerable().Count()));
		}

		[Test]
		public void SelectManyLeftJoinCount()
		{
			var expected =
				from p in Parent
				from c in p.Children.Select(o => new { o.ChildID, p.ParentID }).DefaultIfEmpty()
				select new { p.Value1, o = c };

			ForEachProvider(db => Assert.AreEqual(expected.Count(),
				(from p in db.Parent
				from c in p.Children.Select(o => new { o.ChildID, p.ParentID }).DefaultIfEmpty()
				select new { p.Value1, n = c.ChildID + 1, o = c }).Count()));
		}

		void Foo(Expression<Func<object[],object>> func)
		{
			/*
				ParameterExpression ps;
				Expression.Lambda<Func<object[],object>>(
					Expression.Add(
						Expression.Convert(
							Expression.ArrayIndex(
								ps = Expression.Parameter(typeof(object[]), "p"),
								Expression.Constant(0, typeof(int))),
							typeof(string)),
						Expression.Convert(
							Expression.Convert(
								Expression.ArrayIndex(
									ps,
									Expression.Constant(1, typeof(int))),
								typeof(int)),
							typeof(object)),
						(MethodInfo)methodof(string.Concat)),
					new ParameterExpression[] { ps });
			*/
		}

		Dictionary<string,string> _dic = new Dictionary<string,string>();

		void Bar()
		{
			Foo(p => (string)p[0] + (int)p[1]);
		}

		//[Test]
		public void Test___()
		{
			Bar();
		}
	}
}
