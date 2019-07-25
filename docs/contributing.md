We'll be glad to accept patches and contributions to the project. There are just few guidelines we ask to follow.

Contribution License Agreement
==============================

If you want/plan to contribute, we ask you to sign a [CLA](https://cla.microsoft.com/) (Contribution License Agreement).  A friendly bot will remind you about it when you submit a pull-request.

Submitting a contribution
=========================

It's generally best to start by [opening a new issue](https://help.github.com/articles/creating-an-issue) describing the work you intend to submit. Even for minor tasks, it's helpful to know what contributors are working on. Please mention in the initial issue that you are planning to work on it, so that it can be assigned to you.

Follow the usual GitHub flow process of [forking the project](https://help.github.com/articles/fork-a-repo), and setup a new branch to work in. Each group of changes should be done in separate branches, in order to ensure that a pull request only includes the changes related to one issue.

Any significant change should almost always be accompanied by tests. Look at the existing tests to see the testing approach and style used.  

Follow the project coding style, to ensure consistency and quick code reviews.  We heavily rely on dependency injection using *Autofac* and thus ask to follow the same paradigm when adding new code.

Do your best to have clear commit messages for each change, in order to keep consistency throughout the project. Reference the issue number (#num). A good commit message serves at least these purposes:
* Speed up the pull request review process
* Help future developers to understand the purpose of your code
* Help the maintainer write release notes

One-line messages are fine for small changes, but bigger changes should look like this:
```
$ git commit -m "A brief summary of the commit
>
> A paragraph describing what changed and its impact."
```

Finally, push the commits to your fork, submit a pull request, wait for all gates to pass and fix any issues found as part of the gate process.  The team might ask for some [changes](https://help.github.com/articles/committing-changes-to-a-pull-request-branch-created-from-a-fork) before merging the pull request.