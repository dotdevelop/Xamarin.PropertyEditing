using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using NUnit.Framework;
using Xamarin.PropertyEditing.ViewModels;

namespace Xamarin.PropertyEditing.Tests
{
	[TestFixture]
	public class SimpleCollectionViewTests
	{
		[Test]
		public void Source()
		{
			TestNode[] nodes = new TestNode[] {
				new TestNode { Key = "key" },
				new TestNode { Key = "key2" }
			};

			var view = new SimpleCollectionView (nodes, new SimpleCollectionViewOptions {
				DisplaySelector = TestNodeDisplaySelector
			});

			Assert.That (view, Contains.Item (nodes[0]));
			Assert.That (view, Contains.Item (nodes[1]));
		}

		[Test]
		public void PassThroughINCC()
		{
			var nodes = new ObservableCollection<TestNode> {
				new TestNode { Key = "key" },
				new TestNode { Key = "key2" }
			};

			var view = new SimpleCollectionView (nodes, new SimpleCollectionViewOptions {
				DisplaySelector = TestNodeDisplaySelector
			});
			bool changed = false;
			NotifyCollectionChangedAction action = NotifyCollectionChangedAction.Reset;
			view.CollectionChanged += (sender, e) => {
				changed = true;
				action = e.Action;
			};

			nodes.Add (new TestNode { Key = "key3" });
			Assert.That (changed, Is.True);
			Assert.That (action, Is.EqualTo (NotifyCollectionChangedAction.Add).Or.EqualTo (NotifyCollectionChangedAction.Reset));
			changed = false;

			nodes.RemoveAt (0);
			Assert.That (changed, Is.True);
			Assert.That (action, Is.EqualTo (NotifyCollectionChangedAction.Remove).Or.EqualTo (NotifyCollectionChangedAction.Reset));
			changed = false;

			nodes.Move (0, 1);
			Assert.That (changed, Is.True);
			Assert.That (action, Is.EqualTo (NotifyCollectionChangedAction.Move).Or.EqualTo (NotifyCollectionChangedAction.Reset));
			changed = false;

			nodes[0] = new TestNode { Key = "replace" };
			Assert.That (changed, Is.True);
			Assert.That (action, Is.EqualTo (NotifyCollectionChangedAction.Replace).Or.EqualTo (NotifyCollectionChangedAction.Reset));
			changed = false;

			nodes.Clear();
			Assert.That (changed, Is.True);
			Assert.That (action, Is.EqualTo (NotifyCollectionChangedAction.Reset));
			changed = false;
		}

		[Test]
		public void Filter()
		{
			TestNode[] nodes = new TestNode[] {
				new TestNode { Key = "key" },
				new TestNode { Key = "key2" }
			};

			var view = new SimpleCollectionView (nodes, new SimpleCollectionViewOptions {
				DisplaySelector = TestNodeDisplaySelector
			});

			Assume.That (view, Contains.Item (nodes[0]));
			Assume.That (view, Contains.Item (nodes[1]));

			bool changed = false;
			view.CollectionChanged += (sender, e) => {
				changed = true;
				Assert.That (e.Action, Is.EqualTo (NotifyCollectionChangedAction.Remove).Or.EqualTo (NotifyCollectionChangedAction.Reset));
				if (e.Action == NotifyCollectionChangedAction.Remove) {
					Assert.That (e.OldItems, Contains.Item (nodes[0]));
					Assert.That (e.OldStartingIndex, Is.EqualTo (0));
				}
			};

			view.Options.Filter = o => ((TestNode)o).Key.StartsWith ("key");
			Assert.That (changed, Is.False);
			Assert.That (view, Contains.Item (nodes[0]));
			Assert.That (view, Contains.Item (nodes[1]));

			view.Options.Filter = o => ((TestNode)o).Key.StartsWith ("key2");
			Assert.That (changed, Is.True);
			Assert.That (view, Does.Not.Contain (nodes[0]));
			Assert.That (view, Contains.Item (nodes[1]));
		}

		[Test]
		public void FilterGroupedIndexes()
		{
			TestNode[] nodes = new TestNode[] {
				new TestNode ("A"),
				new TestNode ("Ab") { Flag = true },
				new TestNode ("Ac") { Flag = true },
				new TestNode ("Ad"),
				new TestNode ("Ae") { Flag = true },
				new TestNode ("Af") { Flag = true }
			};

			var view = new SimpleCollectionView (nodes, new SimpleCollectionViewOptions {
				DisplaySelector = TestNodeDisplaySelector
			});

			Assume.That (view.Cast<object>().Count(), Is.EqualTo (6));

			List<NotifyCollectionChangedEventArgs> args = new List<NotifyCollectionChangedEventArgs>();
			view.CollectionChanged += (sender, e) => {
				args.Add (e);
			};

			view.Options.Filter = o => !((TestNode)o).Flag;

			Assert.That (view, Contains.Item (nodes[0]));
			Assert.That (view, Contains.Item (nodes[3]));
			Assert.That (view.Cast<object>().Count(), Is.EqualTo (2));

			Assert.That (args.Count, Is.EqualTo (4));
			Assert.That (args[0].OldItems, Contains.Item (nodes[1]));
			Assert.That (args[0].OldStartingIndex, Is.EqualTo (1));
			Assert.That (args[1].OldItems, Contains.Item (nodes[2]));
			Assert.That (args[1].OldStartingIndex, Is.EqualTo (1));
			Assert.That (args[2].OldItems, Contains.Item (nodes[4]));
			Assert.That (args[2].OldStartingIndex, Is.EqualTo (2));
			Assert.That (args[3].OldItems, Contains.Item (nodes[5]));
			Assert.That (args[3].OldStartingIndex, Is.EqualTo (2));
		}

		[Test]
		[Description ("SCV automatically sorts by the display selector")]
		public void InOrder()
		{
			TestNode[] nodes = new TestNode[] {
				new TestNode { Key = "Z" },
				new TestNode { Key = "C" },
				new TestNode { Key = "E" },
				new TestNode { Key = "B" },
				new TestNode { Key = "A" }
			};

			var view = new SimpleCollectionView (nodes, new SimpleCollectionViewOptions {
				DisplaySelector = TestNodeDisplaySelector
			});

			Assume.That (view, Contains.Item (nodes[0]));
			Assume.That (view, Contains.Item (nodes[1]));
			Assume.That (view, Contains.Item (nodes[2]));
			Assume.That (view, Contains.Item (nodes[3]));
			Assume.That (view, Contains.Item (nodes[4]));

			Assert.That (view.ElementAt (0), Is.EqualTo (nodes[4]));
			Assert.That (view.ElementAt (1), Is.EqualTo (nodes[3]));
			Assert.That (view.ElementAt (2), Is.EqualTo (nodes[1]));
			Assert.That (view.ElementAt (3), Is.EqualTo (nodes[2]));
			Assert.That (view.ElementAt (4), Is.EqualTo (nodes[0]));
		}

		[Test]
		public void AddedInOrder()
		{
			var nodes = new ObservableCollection<TestNode> {
				new TestNode { Key = "Z" },
				new TestNode { Key = "C" },
				// F
				new TestNode { Key = "E" },
				new TestNode { Key = "B" },
				new TestNode { Key = "A" }
			};

			var view = new SimpleCollectionView (nodes, new SimpleCollectionViewOptions {
				DisplaySelector = TestNodeDisplaySelector
			});

			Assume.That (view.ElementAt (0), Is.EqualTo (nodes[4]));
			Assume.That (view.ElementAt (1), Is.EqualTo (nodes[3]));
			Assume.That (view.ElementAt (2), Is.EqualTo (nodes[1]));
			Assume.That (view.ElementAt (3), Is.EqualTo (nodes[2]));
			Assume.That (view.ElementAt (4), Is.EqualTo (nodes[0]));

			bool changed = false;
			NotifyCollectionChangedAction action = NotifyCollectionChangedAction.Reset;
			view.CollectionChanged += (sender, e) => {
				changed = true;
				action = e.Action;
			};

			nodes.Insert (2, new TestNode ("F"));
			Assert.That (action, Is.EqualTo (NotifyCollectionChangedAction.Add).Or.EqualTo (NotifyCollectionChangedAction.Reset));

			Assert.That (changed, Is.True);
			Assert.That (view.ElementAt (0), Is.EqualTo (nodes[5]));
			Assert.That (view.ElementAt (1), Is.EqualTo (nodes[4]));
			Assert.That (view.ElementAt (2), Is.EqualTo (nodes[1]));
			Assert.That (view.ElementAt (3), Is.EqualTo (nodes[3]));
			Assert.That (view.ElementAt (4), Is.EqualTo (nodes[2]));
			Assert.That (view.ElementAt (5), Is.EqualTo (nodes[0]));
		}

		[Test]
		public void Grouping()
		{
			TestNode[] nodes = new TestNode[] {
				new TestNode {
					Key = "Group",
					Children = new List<TestNode> {
						new TestNode ("Child")
					}
				},

				new TestNode {
					Key = "Group2",
					Children = new List<TestNode> {
						new TestNode ("ChildB"),
						new TestNode ("ChildB2")
					}
				}
			};

			var view = new SimpleCollectionView (nodes, new SimpleCollectionViewOptions {
				DisplaySelector = TestNodeDisplaySelector,
				ChildrenSelector = TestNodeChildrenSelector,
				ChildOptions = new SimpleCollectionViewOptions {
					DisplaySelector = TestNodeDisplaySelector
				}
			});

			Assert.That (view, Is.Not.Empty);
			var kvp = (KeyValuePair<string, SimpleCollectionView>)view.Cast<object>().First();
			Assert.That (kvp.Value, Is.Not.Empty);
			Assert.That (kvp.Value, Contains.Item (nodes[0].Children[0]));
		}

		[Test]
		[Description ("When a parent's children are filtered out, it should disappear")]
		public void FilterEmptyCategory()
		{
			TestNode[] nodes = new TestNode[] {
				new TestNode {
					Key = "Group",
					Children = new List<TestNode> {
						new TestNode ("Child")
					}
				}
			};

			var view = new SimpleCollectionView (nodes, new SimpleCollectionViewOptions {
					DisplaySelector = TestNodeDisplaySelector,
					ChildrenSelector = TestNodeChildrenSelector,
					ChildOptions = new SimpleCollectionViewOptions {
						DisplaySelector = TestNodeDisplaySelector
					}
				});

			Assume.That (view, Is.Not.Empty);

			view.Options.ChildOptions.Filter = o => ((TestNode)o).Key.StartsWith ("X");

			Assert.That (view, Is.Empty);
		}

		[Test]
		public void FilterEmptyCategoryChildChanges()
		{
			TestNode[] nodes = new TestNode[] {
				new TestNode {
					Key = "Group",
					Children = new List<TestNode> {
						new TestNode ("Child")
					}
				}
			};

			var view = new SimpleCollectionView (nodes, new SimpleCollectionViewOptions {
					DisplaySelector = TestNodeDisplaySelector,
					ChildrenSelector = TestNodeChildrenSelector,
					ChildOptions = new SimpleCollectionViewOptions {
						DisplaySelector = TestNodeDisplaySelector
					}
				});
			Assume.That (view, Is.Not.Empty);

			view.Options.ChildOptions.Filter = o => ((TestNode)o).Key.StartsWith ("X");
			Assume.That (view, Is.Empty);

			view.Options.ChildOptions.Filter = null;
			Assert.That (view, Is.Not.Empty);
		}

		[Test]
		[Description ("Categories that have had their children filtered out should not be shown and should update when items are added externally")]
		public void FilterEmptyCategoryChildAdded()
		{
			var nodes = new ObservableCollection<TestNode> {
				new TestNode {
					Key = "Group",
					Children = new ObservableCollection<TestNode> {
						new TestNode ("Child")
					}
				}
			};

			var view = new SimpleCollectionView (nodes, new SimpleCollectionViewOptions {
				DisplaySelector = TestNodeDisplaySelector,
				ChildrenSelector = TestNodeChildrenSelector,
				ChildOptions = new SimpleCollectionViewOptions {
					DisplaySelector = TestNodeDisplaySelector
				}
			});
			Assume.That (view, Is.Not.Empty);

			view.Options.ChildOptions.Filter = o => ((TestNode)o).Key.StartsWith ("X");
			Assume.That (view, Is.Empty);

			var newNode = new TestNode ("Xing");
			nodes[0].Children.Add (newNode);
			Assert.That (view, Is.Not.Empty);

			var children = ((KeyValuePair<string, SimpleCollectionView>)view.ElementAt(0)).Value;

			Assert.That (children.Count(), Is.EqualTo (1));
			Assert.That (children, Contains.Item (newNode));
		}

		[Test]
		public void FilteredCategory()
		{
			var nodes = new ObservableCollection<TestNode> {
				new TestNode {
					Key = "Group",
					Children = new ObservableCollection<TestNode> {
						new TestNode ("Child")
					}
				}
			};

			var view = new SimpleCollectionView (nodes, new SimpleCollectionViewOptions {
				DisplaySelector = TestNodeDisplaySelector,
				ChildrenSelector = TestNodeChildrenSelector,
				ChildOptions = new SimpleCollectionViewOptions {
					DisplaySelector = TestNodeDisplaySelector
				}
			});
			Assume.That (view, Is.Not.Empty);

			view.Options.Filter = o => ((TestNode)o).Key.StartsWith ("X");
			Assert.That (view, Is.Empty);
		}

		[Test]
		[Description ("A category that's been filtered out should remain so even when children are added.")]
		public void FilteredCategoryChildAdded()
		{
			var nodes = new ObservableCollection<TestNode> {
				new TestNode {
					Key = "Group",
					Children = new ObservableCollection<TestNode> {
						new TestNode ("Child")
					}
				}
			};

			var view = new SimpleCollectionView (nodes, new SimpleCollectionViewOptions {
				DisplaySelector = TestNodeDisplaySelector,
				ChildrenSelector = TestNodeChildrenSelector,
				ChildOptions = new SimpleCollectionViewOptions {
					DisplaySelector = TestNodeDisplaySelector
				}
			});
			Assume.That (view, Is.Not.Empty);

			view.Options.Filter = o => ((TestNode)o).Key.StartsWith ("X");
			Assume.That (view, Is.Empty);

			nodes[0].Children.Add (new TestNode ("Xing"));
			Assert.That (view, Is.Empty);
		}

		[Test]
		public void FilterEmptyCategoryChildRemoved()
		{
			var nodes = new ObservableCollection<TestNode> {
				new TestNode {
					Key = "Group",
					Children = new ObservableCollection<TestNode> {
						new TestNode ("Child1"),
					}
				}
			};

			var view = new SimpleCollectionView (nodes, new SimpleCollectionViewOptions {
				DisplaySelector = TestNodeDisplaySelector,
				ChildrenSelector = TestNodeChildrenSelector,
				ChildOptions = new SimpleCollectionViewOptions {
					DisplaySelector = TestNodeDisplaySelector
				}
			});
			Assume.That (view, Is.Not.Empty);

			view.Options.ChildOptions.Filter = o => ((TestNode)o).Key.StartsWith ("Child");
			Assume.That (view, Is.Not.Empty);

			NotifyCollectionChangedAction? action = null;
			int index = -1;
			IList items = null;
			view.CollectionChanged += (o, e) => {
				action = e.Action;
				index = e.OldStartingIndex;
				items = e.OldItems;
			};

			var kvp = view.Cast<KeyValuePair<string, SimpleCollectionView>>().First();

			NotifyCollectionChangedAction? childAction = null;
			int childIndex = -1;
			IList childItems = null;
			kvp.Value.CollectionChanged += (o, e) => {
				childAction = e.Action;
				childIndex = e.OldStartingIndex;
				childItems = e.OldItems;
			};

			var childItem = nodes[0].Children[0];
			nodes[0].Children.RemoveAt (0);

			Assert.That (action, Is.EqualTo (NotifyCollectionChangedAction.Remove).Or.EqualTo (NotifyCollectionChangedAction.Reset));
			if (action != NotifyCollectionChangedAction.Reset) {
				Assert.That (index, Is.EqualTo (0));
				Assert.That (items, Contains.Item (kvp));
			}

			Assert.That (childAction, Is.EqualTo (NotifyCollectionChangedAction.Remove).Or.EqualTo (NotifyCollectionChangedAction.Reset));
			if (childAction != NotifyCollectionChangedAction.Reset) {
				Assert.That (childIndex, Is.EqualTo (0));
				Assert.That (childItems, Contains.Item (childItem));
			}

			Assert.That (view, Is.Empty);
		}

		[Test]
		public void FilteredChild()
		{
			var nodes = new ObservableCollection<TestNode> {
				new TestNode {
					Key = "Group",
					Children = new ObservableCollection<TestNode> {
						new TestNode ("Child1"),
						new TestNode ("Child2")
					}
				}
			};

			var view = new SimpleCollectionView (nodes, new SimpleCollectionViewOptions {
				DisplaySelector = TestNodeDisplaySelector,
				ChildrenSelector = TestNodeChildrenSelector,
				ChildOptions = new SimpleCollectionViewOptions {
					DisplaySelector = TestNodeDisplaySelector
				}
			});
			Assume.That (view, Is.Not.Empty);

			view.Options.ChildOptions.Filter = o => ((TestNode)o).Key.StartsWith ("Child");
			Assume.That (view, Is.Not.Empty);

			NotifyCollectionChangedAction? action = null;
			int index = -1;
			IList items = null;
			view.CollectionChanged += (o, e) => {
				action = e.Action;
				index = e.OldStartingIndex;
				items = e.OldItems;
			};

			var kvp = view.Cast<KeyValuePair<string, SimpleCollectionView>>().First();

			var childArgs = new List<NotifyCollectionChangedEventArgs> ();
			kvp.Value.CollectionChanged += (o, e) => {
				childArgs.Add (e);
			};

			var childItem = nodes[0].Children[0];
			var childItem2 = nodes[0].Children[1];
			view.Options.ChildOptions.Filter = o => ((TestNode)o).Key.StartsWith ("X");

			Assert.That (action, Is.EqualTo (NotifyCollectionChangedAction.Remove).Or.EqualTo (NotifyCollectionChangedAction.Reset));
			if (action != NotifyCollectionChangedAction.Reset) {
				Assert.That (index, Is.EqualTo (0));
				Assert.That (items, Contains.Item (kvp));
			}

			if (childArgs.Count == 1) {
				Assert.That (childArgs[0].Action, Is.EqualTo (NotifyCollectionChangedAction.Reset));
			} else {
				Assert.That (childArgs.Count, Is.EqualTo (2));
				for (int i = 0; i < childArgs.Count; i++) {
					Assert.That (childArgs[i].Action, Is.EqualTo (NotifyCollectionChangedAction.Remove));
					Assert.That (childArgs[i].OldStartingIndex, Is.EqualTo (0));
					Assert.That (childArgs[i].OldItems, Contains.Item ((i == 0) ? childItem : childItem2));
				}
			}

			Assert.That (view, Is.Empty);
		}

		[Test]
		public void FilteredChildTwoLevels()
		{
			var nodes = new ObservableCollection<TestNode> {
				new TestNode {
					Key = "Group",
					Children = new ObservableCollection<TestNode> {
						new TestNode ("Child1") {
							Children = new[] {
								new TestNode ("Leaf1")
							}
						},
						new TestNode ("Child2") {
							Children = new[] {
								new TestNode ("Leaf2")
							}
						}
					}
				}
			};

			var view = new SimpleCollectionView (nodes, new SimpleCollectionViewOptions {
				DisplaySelector = TestNodeDisplaySelector,
				ChildrenSelector = TestNodeChildrenSelector,
				ChildOptions = new SimpleCollectionViewOptions {
					DisplaySelector = TestNodeDisplaySelector,
					ChildrenSelector = TestNodeChildrenSelector,
					ChildOptions = new SimpleCollectionViewOptions {
						DisplaySelector = TestNodeDisplaySelector
					}
				}
			});
			Assume.That (view, Is.Not.Empty);

			var topKvp = view.Cast<KeyValuePair<string, SimpleCollectionView>>().First();
			var leafKvp = topKvp.Value.Cast<KeyValuePair<string, SimpleCollectionView>>().First();
			var leafItem = leafKvp.Value.Cast<object>().First();

			NotifyCollectionChangedAction? childAction = null;
			int childIndex = -1;
			IList childItems = null;
			topKvp.Value.CollectionChanged += (o, e) => {
				childAction = e.Action;
				childIndex = e.OldStartingIndex;
				childItems = e.OldItems;
			};

			NotifyCollectionChangedAction? leafAction = null;
			int leafIndex = -1;
			IList leafItems = null;
			leafKvp.Value.CollectionChanged += (o, e) => {
				leafAction = e.Action;
				leafIndex = e.OldStartingIndex;
				leafItems = e.OldItems;
			};

			view.Options.ChildOptions.ChildOptions.Filter = o => ((TestNode)o).Key.StartsWith ("Leaf2");

			Assert.That (childAction, Is.EqualTo (NotifyCollectionChangedAction.Remove).Or.EqualTo (NotifyCollectionChangedAction.Reset));
			if (childAction != NotifyCollectionChangedAction.Reset) {
				Assert.That (childIndex, Is.EqualTo (0));
				Assert.That (childItems, Contains.Item (leafKvp));
				Assert.That (childItems.Count, Is.EqualTo (1));
			}

			Assert.That (leafAction, Is.EqualTo (NotifyCollectionChangedAction.Remove).Or.EqualTo (NotifyCollectionChangedAction.Reset));
			if (leafAction != NotifyCollectionChangedAction.Reset) {
				Assert.That (leafIndex, Is.EqualTo (0));
				Assert.That (leafItems, Contains.Item (leafItem));
				Assert.That (leafItems.Count, Is.EqualTo (1));
			}

			Assert.That (view, Is.Not.Empty);
			var top = view.Cast<KeyValuePair<string, SimpleCollectionView>>().First();
			Assert.That (top.Key, Is.EqualTo (nodes[0].Key));
			var item = top.Value.Cast<KeyValuePair<string, SimpleCollectionView>>().First();
			Assert.That (item.Key, Is.EqualTo (nodes[0].Children[1].Key));
		}

		[Test]
		public void AddBackInUnordered()
		{
			var nodes = new ObservableCollection<TestNode> {
				new TestNode ("B"),
				new TestNode ("Xamarin"),
				new TestNode ("A")
			};

			var view  = new SimpleCollectionView (nodes, new SimpleCollectionViewOptions {
				DisplaySelector = TestNodeDisplaySelector
			});

			Assume.That (view.Cast<TestNode>().ElementAt (0), Is.EqualTo (nodes[2]));
			Assume.That (view.Cast<TestNode>().ElementAt (1), Is.EqualTo (nodes[0]));
			Assume.That (view.Cast<TestNode>().ElementAt (2), Is.EqualTo (nodes[1]));

			view.Options.Filter = o => ((TestNode)o).Key.StartsWith ("X");

			Assume.That (view.Cast<TestNode>().Count(), Is.EqualTo (1));
			Assume.That (view.Cast<TestNode>().ElementAt (0), Is.EqualTo (nodes[1]));

			view.Options.Filter = null;

			Assert.That (view.Cast<TestNode>().ElementAt (0), Is.EqualTo (nodes[2]));
			Assert.That (view.Cast<TestNode>().ElementAt (1), Is.EqualTo (nodes[0]));
			Assert.That (view.Cast<TestNode>().ElementAt (2), Is.EqualTo (nodes[1]));
		}

		[Test]
		public void AddBackInUnorderedPartial()
		{
			var nodes = new ObservableCollection<TestNode> {
				new TestNode ("B"),
				new TestNode ("Xamarin"),
				new TestNode ("A"),
				new TestNode ("Xamagon"),
			};

			var view  = new SimpleCollectionView (nodes, new SimpleCollectionViewOptions {
				DisplaySelector = TestNodeDisplaySelector
			});

			Assume.That (view.Cast<TestNode>().ElementAt (0), Is.EqualTo (nodes[2]));
			Assume.That (view.Cast<TestNode>().ElementAt (1), Is.EqualTo (nodes[0]));
			Assume.That (view.Cast<TestNode>().ElementAt (2), Is.EqualTo (nodes[3]));
			Assume.That (view.Cast<TestNode>().ElementAt (3), Is.EqualTo (nodes[1]));

			view.Options.Filter = o => ((TestNode)o).Key.StartsWith ("Xamarin");

			Assume.That (view.Cast<TestNode>().Count(), Is.EqualTo (1));
			Assume.That (view.Cast<TestNode>().ElementAt (0), Is.EqualTo (nodes[1]));

			view.Options.Filter = o => ((TestNode)o).Key.StartsWith ("Xama");

			Assert.That (view.Cast<TestNode>().ElementAt (0), Is.EqualTo (nodes[3]));
			Assert.That (view.Cast<TestNode>().ElementAt (1), Is.EqualTo (nodes[1]));
		}

		[Test]
		public void SettingEvenSameFilterTriggersUpdate ()
		{
			var nodes = new ObservableCollection<TestNode> {
				new TestNode ("Baz"),
				new TestNode ("Bar"),
				new TestNode ("A")
			};

			var view  = new SimpleCollectionView (nodes, new SimpleCollectionViewOptions {
				DisplaySelector = TestNodeDisplaySelector
			});

			string textFilter = "B";
			Predicate<object> filter = o => ((TestNode) o).Key.StartsWith (textFilter, StringComparison.OrdinalIgnoreCase);
			view.Options.Filter = filter;

			Assume.That (view, Contains.Item (nodes[0]));
			Assume.That (view, Contains.Item (nodes[1]));
			Assume.That (view, Does.Not.Contain (nodes[2]));

			textFilter = "Bar";
			view.Options.Filter = filter;
			Assert.That (view, Does.Not.Contain (nodes[0]));
			Assert.That (view, Contains.Item (nodes[1]));
			Assert.That (view, Does.Not.Contain (nodes[2]));
		}

		[Test]
		public void IndexOf ()
		{
			var nodes = new ObservableCollection<TestNode> {
				new TestNode ("A"),
				new TestNode ("Baz"),
				new TestNode ("Bar"),
			};

			var view = new SimpleCollectionView (nodes, new SimpleCollectionViewOptions {
				DisplaySelector = TestNodeDisplaySelector
			});

			Assume.That (view, Contains.Item (nodes[0]));
			Assume.That (view, Contains.Item (nodes[1]));
			Assume.That (view, Contains.Item (nodes[2]));

			Assert.That (view.IndexOf (nodes[0]), Is.EqualTo (0), "IndexOf didn't return the re-sorted order index");
			Assert.That (view.IndexOf (nodes[1]), Is.EqualTo (2), "IndexOf didn't return the re-sorted order index");
			Assert.That (view.IndexOf (nodes[2]), Is.EqualTo (1), "IndexOf didn't return the re-sorted order index");

			string textFilter = "B";
			view.Options.Filter = o => ((TestNode)o).Key.StartsWith (textFilter, StringComparison.OrdinalIgnoreCase);

			Assume.That (view, Does.Not.Contain (nodes[0]));
			Assume.That (view, Contains.Item (nodes[1]));
			Assume.That (view, Contains.Item (nodes[2]));

			Assert.That (view.IndexOf (nodes[0]), Is.EqualTo (-1), "IndexOf didn't return not-found for filtered out item");
			Assert.That (view.IndexOf (nodes[1]), Is.EqualTo (1), "IndexOf didn't return the filtred order index");
			Assert.That (view.IndexOf (nodes[2]), Is.EqualTo (0), "IndexOf didn't return the filtered order index");
		}

		[Test]
		public void Indexer ()
		{
			var nodes = new ObservableCollection<TestNode> {
				new TestNode ("A"),
				new TestNode ("Baz"),
				new TestNode ("Bar"),
			};

			var view = new SimpleCollectionView (nodes, new SimpleCollectionViewOptions {
				DisplaySelector = TestNodeDisplaySelector
			});

			Assume.That (view, Contains.Item (nodes[0]));
			Assume.That (view, Contains.Item (nodes[1]));
			Assume.That (view, Contains.Item (nodes[2]));

			Assert.That (view[0], Is.EqualTo (nodes[0]), "this[] didn't return the re-sorted order index");
			Assert.That (view[2], Is.EqualTo (nodes[1]), "this[] didn't return the re-sorted order index");
			Assert.That (view[1], Is.EqualTo (nodes[2]), "this[] didn't return the re-sorted order index");

			string textFilter = "B";
			view.Options.Filter = o => ((TestNode)o).Key.StartsWith (textFilter, StringComparison.OrdinalIgnoreCase);

			Assume.That (view, Does.Not.Contain (nodes[0]));
			Assume.That (view, Contains.Item (nodes[1]));
			Assume.That (view, Contains.Item (nodes[2]));

			Assert.That (view[0], Is.EqualTo (nodes[2]));
			Assert.That (view[1], Is.EqualTo (nodes[1]));
			Assert.Throws<ArgumentOutOfRangeException> (() => view[2].ToString(), "this[] didn't throw out of range on filtered out item");
		}

		[Test]
		[Description ("Covers #522, empty parents now unfiltered by their own filter shouldn't be added")]
		public void ParentFilterChangesOnEmpty ()
		{
			var parents = new ObservableCollection<TestNode> {
				new TestNode ("A") {
					Children = new List<TestNode> {
						new TestNode ("A child")
					}
				},
				new TestNode ("Baz") {
					Children = new List<TestNode> {
						new TestNode ("Baz child")
					}
				},
				new TestNode ("Bar") {
					Children = new List<TestNode> {
						new TestNode ("Bar child")
					}
				},
			};

			var view = new SimpleCollectionView (parents, new SimpleCollectionViewOptions {
				DisplaySelector = TestNodeDisplaySelector,
				ChildrenSelector = TestNodeChildrenSelector,
				ChildOptions = new SimpleCollectionViewOptions {
					DisplaySelector = TestNodeDisplaySelector
				}
			});

			Assume.That (view.Count, Is.EqualTo (3));

			view.Options.Filter = o => ((TestNode) o).Key != "Bar";
			Assume.That (view.Count, Is.EqualTo (2));

			view.Options.ChildOptions.Filter = o => ((TestNode) o).Key.StartsWith ("A");
			Assume.That (view.Count, Is.EqualTo (1));

			view.Options.Filter = null;

			Assert.That (((KeyValuePair<string, SimpleCollectionView>) view[0]).Key, Is.EqualTo (parents[0].Key));
			Assert.That (view.Count, Is.EqualTo (1));
		}

		private IEnumerable TestNodeChildrenSelector (object o)
		{
			return ((TestNode)o).Children;
		}

		private string TestNodeDisplaySelector (object o)
		{
			return ((TestNode)o).Key;
		}

		private class TestNode
		{
			public TestNode()
			{

			}

			public TestNode (string key)
			{
				Key = key;
			}

			public string Key;
			public IList<TestNode> Children;
			public bool Flag;
		}
	}
}
